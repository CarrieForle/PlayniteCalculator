using CommonPluginsShared.Controls;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Calculator
{
	public class Calculator : GenericPlugin
	{
		private static readonly ILogger logger = LogManager.GetLogger();
		private CalculatorSettingsViewModel settings;
		private readonly ItadApi api;

		public override Guid Id { get; } = Guid.Parse("e5da871f-1b18-424b-8bea-ab829b044da9");

		public Calculator(IPlayniteAPI api) : base(api)
		{
			settings = new CalculatorSettingsViewModel(this);
			Properties = new GenericPluginProperties
			{
				HasSettings = true
			};
			this.api = new ItadApi();
		}

		public override ISettings GetSettings(bool firstRunSettings)
		{
			return settings;
		}

		public override UserControl GetSettingsView(bool firstRunSettings)
		{
			return new CalculatorSettingsView();
		}

		public override IEnumerable<SidebarItem> GetSidebarItems()
		{
			yield return new SidebarItem
			{
				Opened = () =>
				{
					var control = new SidebarItemControl();
					control.SetTitle(ResourceProvider.GetString("LOCCalculator"));
					control.AddContent(new SidebarView(settings, PlayniteApi, null));
					return control;
					var games = PlayniteApi.Database.Games;
					IDictionary<Game, HistoricalLowOutput> historicalLows = null;

					var actionRes = PlayniteApi.Dialogs.ActivateGlobalProgress(async (args) => 
					{
						historicalLows = await GetHistoricalLow(games);
					}, new GlobalProgressOptions("Helping you get disappointed in your life..."));

					if (
						!(historicalLows is null) &&
						(actionRes.Result ?? false) && 
						actionRes.Error is null
					)
					{
						return new SidebarView(settings, PlayniteApi, historicalLows);
					}
					else
					{
						return new SidebarErrorView(actionRes.Error);
					}
				},
				Closed = () =>
				{
					// TODO
				},
				Icon = "Assets/sidebar-icon.png",
				Type = SiderbarItemType.View,
				Visible = true,
			};
		}

		private async Task<IDictionary<Game, HistoricalLowOutput>> GetHistoricalLow(ICollection<Game> games)
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

			var historicalLowOutputs = await api.HistoricalLow(historicalLowInputs);
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

			return res;
		}
	}
}