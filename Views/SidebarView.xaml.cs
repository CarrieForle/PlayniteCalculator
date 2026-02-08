using CommonPluginsShared.Controls;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using static Calculator.PlaytimeHelper;

namespace Calculator
{
	public partial class SidebarView : UserControl
	{
		private readonly ICalculator plugin;
		private readonly Game[] playedGames;
		public ICollection<Game> Games { get; }
		public ICollection<GameView> GameViews { get; }
		public CalculatorSettings Settings { get; }
		public IPlayniteAPI PlayniteApi { get; }
		public IDictionary<Game, HistoricalLowOutput> HistoricalLows { get; }

		public double TotalSpentIfRegularPrice { get; private set; }
		public double TotalSpentIfDiscountedPrice { get; private set; }
		public double AveragePrice { get; private set; }
		public double PricePerHour { get; private set; }
		public ulong TotalPlaytime { get; private set; }

		public string GamesPlayedInfo => Localized("LOCCalculatorSidebarGamesPlayed", playedGames.Count(), Games.Count);
		public double PlayedGamesRatio => (double)playedGames.Count() / Games.Count;

		private SidebarView(ICalculator plugin, CalculatorSettings settings, IPlayniteAPI api, IDictionary<Game, HistoricalLowOutput> historicalLows)
		{
			this.plugin = plugin;
			Settings = settings;
			PlayniteApi = api;
			HistoricalLows = historicalLows;
			Games = historicalLows.Keys;
			GameViews = historicalLows.Select(pair =>
			{
				Game game = pair.Key;
				double price = pair.Value.price;

				return new GameView(game, price);
			}).ToArray();

			playedGames = PlayniteApi.Database.Games.Where(g => g.Playtime >= 1).ToArray();

			Calculate(historicalLows);

			var playtimeConverter = new PlaytimeConverter(Settings.PlaytimeDisplayFormat, Settings.PlaytimePaddingZero);
			Resources.Add("PlaytimeConverter", playtimeConverter);

			DataContext = this;
			InitializeComponent();
		}

		internal void Calculate(IDictionary<Game, HistoricalLowOutput> historicalLows)
		{
			TotalSpentIfRegularPrice = HistoricalLows.Sum(pair => pair.Value.price);
			TotalSpentIfDiscountedPrice = HistoricalLows.Sum(pair => pair.Value.lowPrice);
			AveragePrice = HistoricalLows.Average(pair => pair.Value.price);
			TotalPlaytime = PlayniteApi.Database.Games.Aggregate(
				0UL,
				(a, g) => a + g.Playtime
			);
			PricePerHour = TotalSpentIfRegularPrice / (TotalPlaytime / 3600.0);
		}

		public static UserControl Create(ICalculator plugin, CalculatorSettings settings, IPlayniteAPI api, IDictionary<Game, HistoricalLowOutput> historicalLows)
		{
			var instance = new SidebarView(plugin, settings, api, historicalLows);
			var control = new SidebarItemControl();
			control.SetTitle(ResourceProvider.GetString("LOCCalculator"));
			control.AddContent(instance);
			Button refreshBtn = new Button
			{
				Content = "Refresh",
				Command = new RelayCommand(() =>
				{
					api.Dialogs.ActivateGlobalProgress(async (args) =>
					{
						instance.Calculate(await plugin.GetHistoricalLow(api.Database.Games));
					}, new GlobalProgressOptions(ResourceProvider.GetString("LOCCalculatorItadRequestDialog")));
				})
			};

			control.AddHeader(refreshBtn);

			return control;
		}

		private static string Localized(string key, params object[] param)
		{
			return string.Format(ResourceProvider.GetString(key), param);
		}
	}

	public class NumberFormatConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			double amount = (double)values[0];
			string format = values[1].ToString();

			return string.Format(format, amount);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
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

	public class MoneyBinding : MarkupExtension
	{
		public string Path { get; set; }

		public MoneyBinding(string path)
		{
			Path = path;
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			var multiBinding = new MultiBinding
			{
				Converter = new NumberFormatConverter(),
				Mode = BindingMode.OneTime,
			};

			multiBinding.Bindings.Add(new Binding(Path));
			multiBinding.Bindings.Add(new Binding("Settings.MoneyFormat"));

			return multiBinding.ProvideValue(serviceProvider);
		}
	}

	public class GameView
	{
		private Game game;
		private double price;

		public GameView(Game game, double price)
		{
			this.game = game;
			this.price = price;
		}

		public string Name => game.Name;
		public string Icon => game.Icon;
		public ulong Playtime => game.Playtime;
		public double Price => price;
	}
}
