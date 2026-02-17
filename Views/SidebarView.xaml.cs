using CommonPluginsShared.Controls;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Calculator
{
	using static PlaytimeHelper;
	public partial class SidebarView : UserControl
	{
		private readonly ICalculator plugin;
		public CalculatorSettings Settings { get; }
		public IPlayniteAPI PlayniteApi { get; }
		private SidebarViewObject model;
		public SidebarViewObject Model 
		{ 
			get
			{
				return model;
			}
			set
			{
				model = value;
				SetProperties();
			}
		}

		public double TotalSpentIfRegularPrice { get; private set; }
		public double TotalSpentIfDiscountedPrice { get; private set; }
		public double AveragePrice { get; private set; }
		public double PricePerHour { get; private set; }
		public ulong TotalPlaytime { get; private set; }

		public double PlayedGamesRatio { get; private set; }
		public Game[] PlayedGames { get; private set; }
		public ICollection<Game> Games { get; private set; }

		/// <summary>
		/// Games who has an ITAD price entry. 
		/// It doesn't meat it's paid.
		/// </summary>
		public ICollection<PricedGames> PricedGames { get; private set; }

		public IEnumerable<PlaytimeGroup> GamesByPlaytime => GroupBy<PlaytimeGroup, Game>(
			Games,
			new Dictionary<string, Func<Game, bool>>
			{
				["25 or more hours"] = g => g.Playtime >= 25 * 3600,
				["12 to 25 hours"] = g => g.Playtime >= 12 * 3600,
				["6 to 12 hours"] = g => g.Playtime >= 6 * 3600,
				["3 to 6 hours"] = g => g.Playtime >= 3 * 3600,
				["2 to 3 hours"] = g => g.Playtime >= 2 * 3600,
				["1 to 2 hours"] = g => g.Playtime >= 3600,
				["0 to 1 hours"] = g => g.Playtime > 0,
			},
			ResourceProvider.GetString("LOCCalculatorSidebarLowestPlaytimeLevel"),
			playtime => playtime.Sum++
		);

		public IEnumerable<CostGroup> GamesByCost
		{
			get
			{
				var costLevel = new double[] {
					55 * Model.ExchangeRate,
					40 * Model.ExchangeRate,
					25 * Model.ExchangeRate,
					10 * Model.ExchangeRate,
					6 * Model.ExchangeRate,
					2 * Model.ExchangeRate,
					0,
				};

				var costLevelString = new string[costLevel.Length];
				costLevelString[0] = Localized("LOCCalculatorSidebarHighestCostLevel", string.Format(Settings.MoneyFormat, costLevel[0] + .01));
				for (int i = 1; i < costLevel.Length; i++)
				{
					string low = string.Format(Settings.MoneyFormat, costLevel[i] + .01);
					string high = string.Format(Settings.MoneyFormat, costLevel[i - 1]);
					costLevelString[i] = $"{low} - {high}";
				}

				var res = GroupBy<CostGroup, PricedGames>(
					PricedGames,
					new Dictionary<string, Func<PricedGames, bool>>
					{
						[costLevelString[0]] = g => g.price >= costLevel[0],

						[costLevelString[1]] = g => g.price >= costLevel[1],

						[costLevelString[2]] = g => g.price >= costLevel[2],

						[costLevelString[3]] = g => g.price >= costLevel[3],

						[costLevelString[4]] = g => g.price >= costLevel[4],

						[costLevelString[5]] = g => g.price >= costLevel[5],

						[costLevelString[6]] = g => g.price > 0,
					},
					ResourceProvider.GetString("LOCCalculatorSidebarLowestCostLevel"),
					price => price.Sum++
				);
				res.Add(new CostGroup
				{
					Group = ResourceProvider.GetString("LOCCalculatorSidebarUnknown"),
					Sum = Model.UnknownGames.Count,
				});

				int acc = 0;
				foreach (var r in res)
				{
					acc += r.Sum;
					r.Acc = acc;
				}

				return res;
			}
		}

		public IEnumerable<KindOnPlaytimeGroup> PlaytimeByKind
		{
			get
			{
				var res = GroupBy<KindOnPlaytimeGroup, PricedGames>(
					PricedGames,
					new Dictionary<string, Func<PricedGames, bool>>
					{
						["In paid games"] = g => g.price > 0,
					},
					"In free games",
					playtime => playtime.Sum++
				);

				res.Add(new KindOnPlaytimeGroup
				{
					Group = ResourceProvider.GetString("LOCCalculatorSidebarUnknown"),
					Sum = Model.UnknownGames.Aggregate(
						0UL,
						(a, g) => a + g.Playtime
					),
				});

				res.Add(new KindOnPlaytimeGroup
				{
					Group = "Total",
					Sum = TotalPlaytime,
				});

				return res;
			}
		}

		public IEnumerable<PricePerHourGroup> AveragePricePerHour
		{
			get
			{
				return new List<PricePerHourGroup>
				{
					new PricePerHourGroup
					{
						Group = "All games",
						Average = PricePerHour,
					},
					new PricePerHourGroup
					{
						Group = "Only paid games",
						Average = PricedGames
							.Where(g => g.price > 0)
							.Sum(g => g.price) / (TotalPlaytime / 3600),
					},
					new PricePerHourGroup
					{
						Group = "Only played games",
						Average = PricedGames
							.Where(g => g.game.Playtime > 0)
							.Sum(g => g.price) / (TotalPlaytime / 3600),
					},
				};
			}
		}

		public IEnumerable<GameView> TopOwnedGames
		{
			get
			{
				var a = PricedGames;
				var rb = Model.UnknownGames;

				var pricedGameViews =
					from g in a
					select new GameView(g, PlayniteApi);

				var unknownGameViews =
					from g in rb
					select new GameView(g, PlayniteApi);

				return pricedGameViews
					.Union(unknownGameViews)
					.OrderBy(g => g.PricePerHour ?? double.MaxValue)
					.Take(10);
			}
		}

		private SidebarView(ICalculator plugin, CalculatorSettings settings, IPlayniteAPI api, SidebarViewObject model)
		{
			this.plugin = plugin;
			Settings = settings;
			PlayniteApi = api;
			Model = model;

			var playtimeConverter = new PlaytimeConverter(Settings.PlaytimeDisplayFormat, Settings.PlaytimePaddingZero);
			var moneyFormatConverter = new MoneyFormatConverter(Settings.MoneyFormat);
			Resources["PlaytimeConverter"] = playtimeConverter;
			Resources["MoneyFormatConverter"] = moneyFormatConverter;

			DataContext = this;
			InitializeComponent();
			SetGamesPlayedInfo();
		}

		internal void SetGamesPlayedInfo()
		{
			string gamesPlayedText = Localized("LOCCalculatorSidebarGamesPlayed", PlayedGames.Count(), Games.Count());
			string[] parts = Regex.Split(gamesPlayedText, @"\d+", RegexOptions.CultureInvariant);
			var indexes = Regex.Matches(gamesPlayedText, @"\d+", RegexOptions.CultureInvariant)
				.Cast<Match>()
				.Select((match) =>
				{
					if (int.TryParse(match.Value, out int index))
					{
						return index;
					}

					return 0;
				}).ToArray();

			for (int i = 0; i < indexes.Length; i++)
			{
				Run textPart = new Run(parts[i]);
				Bold numberPart = new Bold(new Run(indexes[i].ToString()))
				{
					Foreground = ResourceProvider.GetResource("GlyphBrush") as Brush
				};

				GamesPlayedInfo.Inlines.Add(textPart);
				GamesPlayedInfo.Inlines.Add(numberPart);
			}

			GamesPlayedInfo.Inlines.Add(new Run(parts.Last()));
		}

		private void SetProperties()
		{
			Games = Model.Prices.Keys.Union(Model.UnknownGames).ToArray();
			PlayedGames = Games.Where(g => g.Playtime >= 1).ToArray();
			PricedGames = Model.Prices.Select(pair =>
			{
				Game game = pair.Key;
				double price = pair.Value.price;

				return new PricedGames(game, price);
			}).ToArray();

			AveragePrice = PricedGames.Average(g => g.price);
			TotalSpentIfRegularPrice = PricedGames.Sum(g => g.price);
			TotalSpentIfDiscountedPrice = Model.Prices.Sum(pair => pair.Value.lowPrice);
			TotalPlaytime = Games.Aggregate(
				0UL,
				(a, g) => a + g.Playtime
			);
			PricePerHour = TotalSpentIfRegularPrice / (TotalPlaytime / 3600.0);
			PlayedGamesRatio = (double)PlayedGames.Length / Games.Count;
		}

		public static UserControl Create(ICalculator plugin, CalculatorSettings settings, IPlayniteAPI api, SidebarViewObject historicalLows)
		{
			var instance = new SidebarView(plugin, settings, api, historicalLows);
			var control = new SidebarItemControl();
			control.SetTitle(ResourceProvider.GetString("LOCCalculator"));
			control.AddContent(instance);
			Button refreshBtn = new Button
			{
				Content = ResourceProvider.GetString("LOCCalculatorSidebarRefresh"),
				Command = new RelayCommand(() =>
				{
					var res = api.Dialogs.ActivateGlobalProgress(async (args) =>
					{
						instance.Model = await plugin.GetPrice(api.Database.Games, args.CancelToken);
						instance.SetGamesPlayedInfo();
					}, new GlobalProgressOptions(ResourceProvider.GetString("LOCCalculatorItadRequestDialog"), true));

					if (res.Canceled)
					{
						return;
					}

					if (!(res.Error is null))
					{
						instance.PlayniteApi.Dialogs.ShowErrorMessage($"{ResourceProvider.GetString("LOCCalculatorError")}\n\n{res.Error.Message}", ResourceProvider.GetString("LOCCalculator"));
					}
					else if (!(res.Result ?? false))
					{
						instance.PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCCalculatorUnknownError"), ResourceProvider.GetString("LOCCalculator"));
					}
				})
			};

			control.AddHeader(refreshBtn);
			control.AddHeader(new TextBlock
			{
				Text = Localized("LOCCalculatorSidebarLastUpdated", instance.Model.Datetime),
				VerticalAlignment = VerticalAlignment.Center,
				Style = ResourceProvider.GetResource("BaseTextBlockStyle") as Style,
			});

			return control;
		}

		private static string Localized(string key, params object[] param)
		{
			return string.Format(ResourceProvider.GetString(key), param);
		}

		// Dictionary preserve insertion order provided we never do deletion.
		/// <summary>
		/// Group bunch of games by a certain predicate
		/// </summary>
		/// <typeparam name="GroupT">The type of group</typeparam>
		/// <typeparam name="GameT">The type of things to be grouped</typeparam>
		/// <param name="games">The games to be grouped</param>
		/// <param name="predicates">
		///	Define groups by their names and the condition for each group</param>
		/// <param name="defaultGroup">The fallback group if none predicate matches</param>
		/// <param name="action">What to do after a predicate matches</param>
		/// <returns>Groups</returns>
		private static IList<GroupT> GroupBy<GroupT, GameT>(
			ICollection<GameT> games,
			Dictionary<string, Func<GameT, bool>> predicates,
			string defaultGroup,
			Action<GroupT> action
		)
			where GroupT : Groupable, new()
		{
			var grouping = new Dictionary<string, GroupT>(predicates.Count);
			foreach (string group in predicates.Keys)
			{
				grouping.Add(group, new GroupT
				{
					Group = group,
				});
			}

			grouping.Add(defaultGroup, new GroupT
			{
				Group = defaultGroup,
			});

			foreach (var game in games)
			{
				foreach (var pair in predicates)
				{
					string group = pair.Key;
					var predicate = pair.Value;

					if (predicate(game))
					{
						action(grouping[group]);

						goto NextOuterIteration;
					}
				}

				GroupT _obj = grouping[defaultGroup];
				_obj.Group = defaultGroup;
				action(_obj);

			NextOuterIteration:;
			}

			return grouping.Values.ToList();
		}
	}

	public abstract class Groupable
	{
		public string Group { get; set; }
	}

	public class PlaytimeGroup : Groupable
	{
		public int Sum { get; set; } = 0;
	}

	public class CostGroup : Groupable
	{
		public int Sum { get; set; } = 0;
		public int Acc { get; set; } = 0;
	}

	public class KindOnPlaytimeGroup : Groupable
	{
		public ulong Sum { get; set; } = 0;
	}

	public class PricePerHourGroup : Groupable
	{
		public double Average { get; set; } = 0;
	}

	public class PricedGames
	{
		public readonly Game game;
		public readonly double price;

		public PricedGames(Game game, double price)
		{
			this.game = game;
			this.price = price;
		}
	}

	public class GameView
	{
		private readonly Uri iconSource;
		public double Price { get; }
		public ulong Time { get; }
		public string Name { get; }
		public ImageSource IconSource => new BitmapImage(iconSource)
		{
			DecodePixelHeight = 32,
			DecodePixelWidth = 32,
		};

		public double? PricePerHour
		{
			get
			{
				if (Price == 0 || Time == 0)
				{
					return null;
				}
				else
				{
					return Price / (Time / 3600);
				}
			}
		}

		public GameView(Game game, IPlayniteAPI api)
		{
			Price = 0;
			Time = game.Playtime;
			Name = game.Name;
			try
			{
				iconSource = new Uri($"{api.Database.DatabasePath}\\files\\{game.Icon}");
			}
			catch { }
		}

		public GameView(PricedGames game, IPlayniteAPI api) : this(game.game, api)
		{
			Price = game.price;
		}
	}

	[ValueConversion(typeof(string), typeof(string))]
	public class MoneyFormatConverter : IValueConverter
	{
		private readonly string format;
		public MoneyFormatConverter(string format)
		{
			this.format = format;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			double amount = (double)value;
			return string.Format(format, amount);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	[ValueConversion(typeof(ulong), typeof(string))]
	public class PlaytimeConverter : IValueConverter
	{
		private readonly PlaytimeDisplayFormat mode;
		private readonly bool paddingZero = false;

		public PlaytimeConverter(PlaytimeDisplayFormat mode, bool paddingZero)
		{
			this.mode = mode;
			this.paddingZero = paddingZero;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			ulong seconds = (ulong)value;
			return PlaytimeToString(mode, paddingZero, seconds);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
