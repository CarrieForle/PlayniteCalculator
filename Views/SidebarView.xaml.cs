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
		private Game[] playedGames;
		private ICollection<Game> Games => PlayniteApi.Database.Games;
		public CalculatorSettings Settings { get; }
		public IDictionary<Game, HistoricalLowOutput> HistoricalLows { get; }
		public IPlayniteAPI PlayniteApi { get; }

		private double totalSpentIfRegularPrice;
		public double TotalSpentIfRegularPrice => totalSpentIfRegularPrice;

		private double totalSpentIfDiscountedPrice;
		public double TotalSpentIfDiscountedPrice => totalSpentIfDiscountedPrice;

		private double averagePrice;
		public double AveragePrice => averagePrice;

		private double pricePerHour;
		public double PricePerHour => pricePerHour;

		private ulong totalPlaytime;
		public ulong TotalPlaytime => totalPlaytime;

		public string GamesPlayedInfo => Localized("LOCCalculatorSidebarGamesPlayed", playedGames.Count(), Games.Count);
		public double PlayedGamesRatio => (double)playedGames.Count() / Games.Count;

		public SidebarView(CalculatorSettings settings, IPlayniteAPI api, IDictionary<Game, HistoricalLowOutput> historicalLows)
		{
			Settings = settings;
			PlayniteApi = api;
			HistoricalLows = historicalLows;

			//playedGames = PlayniteApi.Database.Games.Where(g => g.Playtime >= 1).ToArray();
			//totalSpentIfRegularPrice = HistoricalLows.Sum(pair => pair.Value.price);
			//totalSpentIfDiscountedPrice = HistoricalLows.Sum(pair => pair.Value.lowPrice);
			//averagePrice = HistoricalLows.Average(pair => pair.Value.price);
			//totalPlaytime = PlayniteApi.Database.Games.Aggregate(
			//	0UL,
			//	(a, g) => a + g.Playtime
			//);
			//pricePerHour = totalSpentIfRegularPrice / (totalPlaytime / 3600.0);

			playedGames = PlayniteApi.Database.Games.Where(g => g.Playtime >= 1).ToArray();
			totalSpentIfRegularPrice = 4238.32;
			totalSpentIfDiscountedPrice = 372.32;
			averagePrice = 5.32;
			totalPlaytime = 689420;
			pricePerHour = 6.32;

			var playtimeConverter = new PlaytimeConverter(Settings.PlaytimeDisplayMode, Settings.PlaytimePaddingZero);
			Resources.Add("PlaytimeConverter", playtimeConverter);

			DataContext = this;
			InitializeComponent();
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
		private readonly PlaytimeDisplayMode mode;
		private readonly bool paddingZero = false;

		public PlaytimeConverter(PlaytimeDisplayMode mode, bool paddingZero)
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
}
