using CommonPluginsShared.Controls;
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
			FontFamily = (FontFamily)ResourceProvider.GetResource("FontIcoFont"),
			FontSize = 20,
		};

		private CalculatorSettings Settings
		{
			get => settingsVm.Settings;
		}

		private readonly ItadApi api = new ItadApi();

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
				GetHistoricalLowFromDisk();
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
					IDictionary<Game, HistoricalLowOutput> historicalLows = null;

					if (Settings.AutomaticUpdate != AutomaticUpdate.OnEnteringView)
					{
						historicalLows = GetHistoricalLowFromDisk();

						if (!(historicalLows is null))
						{
							return SidebarView.Create(this, Settings, PlayniteApi, historicalLows);
						}
					}

					var actionRes = PlayniteApi.Dialogs.ActivateGlobalProgress(async (args) =>
					{
						historicalLows = await GetHistoricalLow(games);
					}, new GlobalProgressOptions(ResourceProvider.GetString("LOCCalculatorItadRequestDialog")));

					if (
						historicalLows is null ||
						!(actionRes.Result ?? false) ||
						actionRes.Error is Exception
					)
					{
						return SidebarErrorView.Create(actionRes.Error);
					}

					return SidebarView.Create(this, Settings, PlayniteApi, historicalLows);
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

		public async Task<IDictionary<Game, HistoricalLowOutput>> GetHistoricalLow(ICollection<Game> games)
		{
			var DEFAULT_SHOP = ItadShop.Steam;
			var names = games.Select(g => g.Name).ToArray();
			var nameToItadIds = await api.LookUpGameId(names);
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

			var historicalLowOutputs = await api.HistoricalLow(historicalLowInputs, Settings.Country);
			var res = new Dictionary<Game, HistoricalLowOutput>();

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

				res[game] = pair.Value;
			}

			CacheHistoricalLowToDisk(res);

			return res;
		}

		private void CacheHistoricalLowToDisk(IDictionary<Game, HistoricalLowOutput> historicalLows)
		{
			var dict = new Dictionary<Guid, HistoricalLowOutput>(historicalLows.Count);
			foreach (var pair in historicalLows)
			{
				dict[pair.Key.Id] = pair.Value;
			}

			// lock is exception safe: Released even when exception is thrown.
			lock (cacheLock)
			{
				using (FileStream fs = new FileStream(cachePath, FileMode.Create))
				{
					Serialization.ToJsonStream(dict, fs);
				}
			}
		}

		private IDictionary<Game, HistoricalLowOutput> GetHistoricalLowFromDisk()
		{
			Dictionary<Guid, HistoricalLowOutput> cache;

			lock (cacheLock)
			{
				cache = Serialization.FromJsonFile<Dictionary<Guid, HistoricalLowOutput>>(cachePath);
			}

			var historicalLows = new Dictionary<Game, HistoricalLowOutput>();

			foreach (var pair in cache)
			{
				var game = PlayniteApi.Database.Games[pair.Key];
				if (game is null)
				{
					continue;
				}

				historicalLows[game] = pair.Value;
			}

			return historicalLows;
		}
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
		Task<IDictionary<Game, HistoricalLowOutput>> GetHistoricalLow(ICollection<Game> games);
	}
}