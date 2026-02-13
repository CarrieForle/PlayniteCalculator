using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace Calculator
{
	public class Calculator : GenericPlugin, ICalculator
	{
		private static readonly ILogger logger = LogManager.GetLogger();
		private readonly LibraryTracker libraryTracker;
		private readonly CalculatorSettingsViewModel settingsVm;
		private string cachePath;
		private object cacheLock = new object();

		private TextBlock icon = new TextBlock
		{
			Text = "\ue9d8",
			FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily,
			FontSize = 20,
		};

		private CalculatorSettings Settings
		{
			get => settingsVm.Settings;
		}

		public override Guid Id { get; } = Guid.Parse("e5da871f-1b18-424b-8bea-ab829b044da9");

		public Calculator(IPlayniteAPI api) : base(api)
		{
			Properties = new GenericPluginProperties
			{
				HasSettings = true
			};

			settingsVm = new CalculatorSettingsViewModel(this);
			cachePath = $@"{GetPluginUserDataPath()}\cache.json";
			libraryTracker = new LibraryTracker(api);
		}

		public override ISettings GetSettings(bool firstRunSettings)
		{
			return settingsVm;
		}

		public override UserControl GetSettingsView(bool firstRunSettings)
		{
			return new CalculatorSettingsView();
		}

		public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
		{
			if (Settings.AutomaticUpdate == AutomaticUpdate.OnLibraryUpdate && libraryTracker.FoundNewGames)
			{
				_ = GetPrice(PlayniteApi.Database.Games, CancellationToken.None);
				libraryTracker.Reset();
			}
		}

		public override IEnumerable<SidebarItem> GetSidebarItems()
		{
			yield return new SidebarItem
			{
				Opened = () =>
				{
					var games = PlayniteApi.Database.Games;
					SidebarViewObject sidebarViewObject = null;

					if (Settings.AutomaticUpdate != AutomaticUpdate.OnEnteringView)
					{
						sidebarViewObject = FromDisk();

						if (!(sidebarViewObject is null))
						{
							return SidebarView.Create(this, Settings, PlayniteApi, sidebarViewObject);
						}
					}

					if (!games.HasItems())
					{
						return SidebarErrorView.Create(new CalculatorException(ResourceProvider.GetString("LOCCalculatorNoGame")));
					}

					var actionRes = PlayniteApi.Dialogs.ActivateGlobalProgress(async (args) =>
					{
						sidebarViewObject = await GetPrice(games, args.CancelToken);
					}, new GlobalProgressOptions(ResourceProvider.GetString("LOCCalculatorItadRequestDialog"), true));

					if (!(actionRes.Result ?? false) ||
						!(actionRes.Error is null)
					)
					{
						return SidebarErrorView.Create(actionRes.Error);
					}

					return SidebarView.Create(this, Settings, PlayniteApi, sidebarViewObject);
				},
				Icon = icon,
				Type = SiderbarItemType.View,
				Visible = true,
			};
		}

		public async Task<SidebarViewObject> GetPrice(ICollection<Game> games, CancellationToken cancellationToken)
		{
			Debug.Assert(games.HasItems());

			using (HttpClient client = new HttpClient())
			{
				var DEFAULT_SHOP = ItadShop.Steam;
				var names = games.Select(g => g.Name).ToArray();
				var nameToItadIds = await ItadApi.LookUpGameId(client, names, cancellationToken);
				var input = new Dictionary<ItadShop, ICollection<string>>();
				var itadIdsToGames = new Dictionary<string, List<Game>>();

				foreach (Game game in games)
				{
					// Ignore games that does not have ITAD ID.
					if (!nameToItadIds.TryGetValue(game.Name, out string id) || id is null)
					{
						continue;
					}

					var shop = ItadShopExtension.FromGameSource(game.Source) ?? DEFAULT_SHOP;

					if (input.ContainsKey(shop))
					{
						input[shop].Add(id);
					}
					else
					{
						input[shop] = new List<string> { id };
					}

					if (itadIdsToGames.ContainsKey(id))
					{
						itadIdsToGames[id].Add(game);
					}
					else
					{
						itadIdsToGames[id] = new List<Game> { game };
					}
				}

				var output = await ItadApi.PriceOverview(client, input, Settings.Country, cancellationToken);
				var currency = output.currency;
				Task<double> currencyTask = null;
				if (currency != default)
				{
					currencyTask = CurrencyApi.GetExchangeRate(client, currency, cancellationToken);
				}

				var prices = new Dictionary<Game, Price>();

				foreach (var pair in output.price)
				{
					string id = pair.Key.id;
					ItadShop? shop = pair.Key.shop;
					var gamesFromId = itadIdsToGames[id];
					Game game;

					if (gamesFromId.Count == 1)
					{
						game = gamesFromId[0];
					}
					// If there are multiple copies, choose the
					// Game object with the following priority:
					//
					// 1. First Game whose source and ITAD shop
					// matches
					// 2. First Game who has no source
					// 3. First Game in the list
					else
					{
						game = gamesFromId.FirstOrDefault(g =>
						{
							return shop == ItadShopExtension.FromGameSource(g.Source);
						}) ?? gamesFromId.FirstOrDefault(g =>
						{
							return ItadShopExtension.FromGameSource(g.Source) is null;
						}) ?? gamesFromId[0];
						gamesFromId.Remove(game);
					}

					prices[game] = pair.Value;
				}

				double exchangeRate = 1;

				if (!(currencyTask is null))
				{
					exchangeRate = await currencyTask;
				}

				var res = new SidebarViewObject
				{
					Prices = prices,
					Currency = currency,
					ExchangeRate = exchangeRate,
					Datetime = DateTime.Now,
					UnknownGames = games.Except(prices.Keys).ToArray(),
				};

				CacheToDisk(res);
				return res;
			}
		}

		private void CacheToDisk(SidebarViewObject obj)
		{
			var cache = new SidebarViewObjectCache
			{
				Prices = new Dictionary<Guid, Price>(),
				Currency = obj.Currency,
				ExchangeRate = obj.ExchangeRate,
				UnknownGames = obj.UnknownGames.Select(g => g.Id).ToArray(),
				Datetime = obj.Datetime,
			};

			foreach (var pair in obj.Prices)
			{
				cache.Prices[pair.Key.Id] = pair.Value;
			}

			// lock is exception safe: Released even when exception is thrown.
			lock (cacheLock)
			{
				using (FileStream fs = new FileStream(cachePath, FileMode.Create))
				{
					Serialization.ToJsonStream(cache, fs);
				}
			}
		}

		/// <summary>
		/// Get cached <see cref="SidebarViewObject" />
		/// </summary>
		/// <returns><see cref="SidebarViewObject" /> if success, otherwise null</returns>
		private SidebarViewObject FromDisk()
		{
			try
			{
				SidebarViewObjectCache cache = null;

				lock (cacheLock)
				{
					if (!Serialization.TryFromJsonFile(cachePath, out cache))
					{
						return null;
					}
				}

				var sidebarViewObject = new SidebarViewObject
				{
					Prices = new Dictionary<Game, Price>(),
					Currency = cache.Currency,
					ExchangeRate = cache.ExchangeRate,
					Datetime = cache.Datetime,
					UnknownGames = new List<Game>(),
				};

				foreach (var pair in cache.Prices)
				{
					var game = PlayniteApi.Database.Games[pair.Key];
					if (game is null)
					{
						continue;
					}

					sidebarViewObject.Prices[game] = pair.Value;
				}

				foreach (var id in cache.UnknownGames)
				{
					var game = PlayniteApi.Database.Games[id];
					if (game is null)
					{
						continue;
					}

					sidebarViewObject.UnknownGames.Add(game);
				}

				return sidebarViewObject;
			}
			catch
			{
				return null;
			}
		}
	}

	public class SidebarViewObject
	{
		public Dictionary<Game, Price> Prices { get; set; }
		public Currency Currency { get; set; }
		public double ExchangeRate { get; set; }
		public DateTime Datetime { get; set; }
		public ICollection<Game> UnknownGames { get; set; }
	}

	internal class SidebarViewObjectCache
	{
		public Dictionary<Guid, Price> Prices { get; set; }
		public Currency Currency { get; set; }
		public double ExchangeRate { get; set; } = 1;
		public DateTime Datetime { get; set; }
		public ICollection<Guid> UnknownGames { get; set; }
	}

	internal class LibraryTracker
	{
		private List<Game> addedGames = new List<Game>();
		public bool FoundNewGames => addedGames.Count > 0;

		public LibraryTracker(IPlayniteAPI api)
		{
			api.Database.Games.ItemCollectionChanged += (s, e) =>
			{
				foreach (var game in e.RemovedItems)
				{
					addedGames.Remove(game);
				}

				foreach (var game in e.AddedItems)
				{
					addedGames.Add(game);
				}
			};
		}

		public void Reset()
		{
			addedGames = new List<Game>();
		}
	}

	public interface ICalculator
	{
		Task<SidebarViewObject> GetPrice(ICollection<Game> games, CancellationToken cancellationToken);
	}
}