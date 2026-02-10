using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
				_ = GetHistoricalLow(PlayniteApi.Database.Games);
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

					var actionRes = PlayniteApi.Dialogs.ActivateGlobalProgress(async (args) =>
					{
						sidebarViewObject = await GetHistoricalLow(games);
					}, new GlobalProgressOptions(ResourceProvider.GetString("LOCCalculatorItadRequestDialog")));

					if (
						sidebarViewObject is null ||
						!(actionRes.Result ?? false) ||
						actionRes.Error is Exception
					)
					{
						return SidebarErrorView.Create(actionRes.Error);
					}

					return SidebarView.Create(this, Settings, PlayniteApi, sidebarViewObject);
				},
				Closed = () =>
				{
					// TODO
				},
				Icon = icon,
				Type = SiderbarItemType.View,
				Visible = true,
			};
		}

		public async Task<SidebarViewObject> GetHistoricalLow(ICollection<Game> games)
		{
			var country = Settings.Country;
			// country and currency are uppercase.
			var currency = ((Currency[])Enum.GetValues(typeof(Currency))).FirstOrDefault(c => c.ToString().StartsWith(country));
			Task<double> currencyTask = null;

			if (currency != Currency.USD)
			{
				currencyTask = CurrencyApi.GetExchangeRate(currency);
			}

			var DEFAULT_SHOP = ItadShop.Steam;
			var names = games.Select(g => g.Name).ToArray();
			var nameToItadIds = await ItadApi.LookUpGameId(names);
			var historicalLowInputs = new Dictionary<ItadShop, ICollection<string>>();
			var itadIdsToGames = new Dictionary<string, List<Game>>();

			foreach (Game game in games)
			{
				// Ignore games that does not have ITAD ID.
				if (nameToItadIds.TryGetValue(game.Name, out string id) && !(id is null))
				{
					var shop = ItadShopExtension.FromGameSource(game.Source) ?? DEFAULT_SHOP;

					if (historicalLowInputs.ContainsKey(shop))
					{
						historicalLowInputs[shop].Add(id);
					}
					else
					{
						historicalLowInputs[shop] = new List<string> { id };
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
			}

			var historicalLowOutputs = await ItadApi.HistoricalLow(historicalLowInputs, Settings.Country);
			var historicalLows = new Dictionary<Game, HistoricalLowOutput>();

			foreach (var pair in historicalLowOutputs)
			{
				string id = pair.Key.id;
				ItadShop? shop = pair.Key.shop;

				var gamesFromId = itadIdsToGames[id];
				Game game;

				if (gamesFromId.Count == 1)
				{
					game = gamesFromId[0];
				}
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

				historicalLows[game] = pair.Value;
			}

			double exchangeRate = 1;

			if (!(currencyTask is null))
			{
				exchangeRate = await currencyTask;
			}

			var res = new SidebarViewObject
			{
				HistoricalLows = historicalLows,
				Currency = currency,
				ExchangeRate = exchangeRate,
			};

			CacheToDisk(res);
			return res;
		}

		private void CacheToDisk(SidebarViewObject obj)
		{
			var cache = new SidebarViewObjectCache
			{
				HistoricalLows = new Dictionary<Guid, HistoricalLowOutput>(),
				Currency = obj.Currency,
				ExchangeRate = obj.ExchangeRate,
			};

			foreach (var pair in obj.HistoricalLows)
			{
				cache.HistoricalLows[pair.Key.Id] = pair.Value;
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

		private SidebarViewObject FromDisk()
		{
			SidebarViewObjectCache cache = null;

			lock (cacheLock)
			{
				if (!Serialization.TryFromJsonFile(cachePath, out cache))
				{
					return null;
				}
			}

			if (cache is null || cache.HistoricalLows is null)
			{
				return null;
			}

			var sidebarViewObject = new SidebarViewObject
			{
				HistoricalLows = new Dictionary<Game, HistoricalLowOutput>(),
				Currency = cache.Currency,
				ExchangeRate = cache.ExchangeRate,
			};

			foreach (var pair in cache.HistoricalLows)
			{
				var game = PlayniteApi.Database.Games[pair.Key];
				if (game is null)
				{
					continue;
				}

				sidebarViewObject.HistoricalLows[game] = pair.Value;
			}

			return sidebarViewObject;
		}
	}

	public class SidebarViewObject
	{
		public Dictionary<Game, HistoricalLowOutput> HistoricalLows { get; set; }
		public Currency Currency { get; set; }
		public double ExchangeRate { get; set; } = 1;
	}

	internal class SidebarViewObjectCache
	{
		public Dictionary<Guid, HistoricalLowOutput> HistoricalLows { get; set; }
		public Currency Currency { get; set; }
		public double ExchangeRate { get; set; } = 1;
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
		Task<SidebarViewObject> GetHistoricalLow(ICollection<Game> games);
	}
}