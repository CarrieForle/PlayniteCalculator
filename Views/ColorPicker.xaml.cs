using Playnite.SDK;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Calculator.Views
{
	public partial class ColorPicker : UserControl
	{
		private Action<Brush> callback;

		public ColorPicker(Action<Brush> callback, Color defaultColor)
		{
			this.callback = callback;

			DataContext = this;
			InitializeComponent();

			Picker.SelectedColor = defaultColor;
		}

		public RelayCommand<bool> ReturnCallback => new RelayCommand<bool>((save) =>
		{
			if (save && Picker.SelectedColor is Color color)
			{
				callback(new SolidColorBrush(color));
			}
			else
			{
				callback(null);
			}
		});
	}
}
