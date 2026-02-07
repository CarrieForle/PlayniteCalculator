using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Calculator.PlaytimeHelper;

namespace Calculator
{
	public partial class CalculatorSettingsView : UserControl
	{
		public CalculatorSettingsView()
		{
			InitializeComponent();
		}

		private IEnumerable<PlaytimeDisplayMode> PlaytimeValues => Enum.GetValues(typeof(PlaytimeDisplayMode)).Cast<PlaytimeDisplayMode>();

		//private void BtPickColor_Click(object sender, RoutedEventArgs e)
		//{
		//	try
		//	{
		//		var TbControl = ((StackPanel)((FrameworkElement)sender).Parent).Children.OfType<TextBlock>().FirstOrDefault();
		//		var LControl = ((StackPanel)((FrameworkElement)sender).Parent).Children.OfType<Label>().FirstOrDefault();

		//		if (TbControl.Background is SolidColorBrush brush)
		//		{
		//			PART_SelectorColorPicker.SetColors(brush);
		//		}
		//		if (TbControl.Background is LinearGradientBrush linearGradientBrush)
		//		{
		//			PART_SelectorColorPicker.SetColors(linearGradientBrush);
		//		}

		//		PART_SelectorColor.Visibility = Visibility.Visible;
		//		PART_ThemeColor.Visibility = Visibility.Collapsed;
		//	}
		//	catch (Exception ex)
		//	{
		//		Common.LogError(ex, false, true, "ThemeModifier");
		//	}
		//}

		//private void BtRestore_Click(object sender, RoutedEventArgs e)
		//{
		//	try
		//	{
		//		TextBlock tbControl = ((StackPanel)((FrameworkElement)sender).Parent).Children.OfType<TextBlock>().FirstOrDefault();
		//		Label lControl = ((StackPanel)((FrameworkElement)sender).Parent).Children.OfType<Label>().FirstOrDefault();

		//		ThemeElement finded = ThemeDefault.Find(x => x.Name == lControl.Content.ToString());

		//		tbControl.Background = finded.Element;

		//		ThemeClass.SetThemeColor(lControl.Content.ToString(), null, Settings, finded.Element);
		//	}
		//	catch (Exception ex)
		//	{
		//		Common.LogError(ex, false, true, "ThemeModifier");
		//	}
		//}

		//private void BtRestoreDefault_Click(object sender, RoutedEventArgs e)
		//{
		//	try
		//	{
		//		ThemeClass.RestoreColor(ThemeDefault, Settings);

		//		foreach (ThemeElement themeElement in ThemeDefault)
		//		{
		//			object control = FindName("tb" + themeElement.Name);
		//			if (control is TextBlock block)
		//			{
		//				block.Background = themeElement.Element;
		//				ThemeClass.SetThemeColor(themeElement.Name, null, Settings, themeElement.Element);
		//			}
		//			else
		//			{
		//				Logger.Warn($"Bad control {"tb" + themeElement.Name}: {control}");
		//			}
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		Common.LogError(ex, false, true, "ThemeModifier");
		//	}
		//}
	}	
}