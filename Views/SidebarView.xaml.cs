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
		public CalculatorSettings Settings { get; }
		public IDictionary<Game, HistoricalLowOutput> HistoricalLows { get; }
		public IPlayniteAPI PlayniteApi { get; }
		public double TotalSpentIfRegularPrice
		{
			//get => HistoricalLows.Sum(pair => pair.Value.price);
			get => 320.44;
		}

		public double TotalSpentIfDiscountedPrice
		{
			//get => HistoricalLows.Sum(pair => pair.Value.lowPrice);
			get => 30;
		}

		public double AveragePrice
		{
			//get => HistoricalLows.Average(pair => pair.Value.price);
			get => 12.3;
		}

		public double PricePerHour => 5.3;

		// Sum() does not support ulong
		public ulong TotalPlaytime => PlayniteApi.Database.Games.Aggregate(
			0UL,
			(a, g) => a + g.Playtime
		);

		public SidebarView(CalculatorSettings settings, IPlayniteAPI api, IDictionary<Game, HistoricalLowOutput> historicalLows)
		{
			Settings = settings;
			PlayniteApi = api;
			HistoricalLows = historicalLows;
			var playtimeConverter = new PlaytimeConverter(Settings.PlaytimeDisplayMode, Settings.PlaytimePaddingZero);
			Resources.Add("PlaytimeConverter", playtimeConverter);
			DataContext = this;
			InitializeComponent();
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
		private PlaytimeDisplayMode mode;
		private bool paddingZero = false;

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
