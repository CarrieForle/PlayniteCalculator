using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using static Calculator.PlaytimeHelper;
using Calculator.Views;

namespace Calculator
{
	public partial class CalculatorSettingsView : UserControl
	{
		public CalculatorSettingsView()
		{
			InitializeComponent();
		}

		public IEnumerable<PlaytimeDisplayMode> PlaytimeValues => Enum.GetValues(typeof(PlaytimeDisplayMode)).Cast<PlaytimeDisplayMode>();

		private void SwitchToColorPicker(object sender, System.Windows.RoutedEventArgs e)
		{
			Button btn = (Button)sender;
			var callbackView = ColorTab.Content;

			ColorTab.Content = new ColorPicker((brush) =>
			{
				if (!(brush is null))
				{
					btn.Background = brush;
				}

				ColorTab.Content = callbackView;
			}, ((SolidColorBrush)btn.Background).Color);
		}

		public RelayCommand<object> RestoreBrush { get; } = new RelayCommand<object>((_brush) =>
		{
			var brush = (Brush)_brush;
			// TODO
		});

		public RelayCommand RestoreDefaultBrush { get; } = new RelayCommand(() =>
		{
			// TODO
		});
	}	
}