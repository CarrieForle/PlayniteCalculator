using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace Calculator
{
	public partial class SidebarView : UserControl
	{
		private readonly CalculatorSettingsViewModel settingsVm;
		public CalculatorSettings Settings { get => settingsVm.Settings; }
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

		public double AveragePricePerHour
		{
			get => 5.3;
		}

		public int HoursSpent
		{
			get => 4222;
		}

		public SidebarView(CalculatorSettingsViewModel settings, IPlayniteAPI api, IDictionary<Game, HistoricalLowOutput> historicalLows)
		{
			settingsVm = settings;
			PlayniteApi = api;
			HistoricalLows = historicalLows;
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

	public class MoneyBinding : MarkupExtension
	{
		public string Path { get; set; }

		public MoneyBinding(string path)
		{
			Path = path;
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			var multiBinding = new MultiBinding { Converter = new NumberFormatConverter() };

			multiBinding.Bindings.Add(new Binding(Path));
			multiBinding.Bindings.Add(new Binding("Settings.MoneyFormat"));

			return multiBinding.ProvideValue(serviceProvider);
		}
	}
}
