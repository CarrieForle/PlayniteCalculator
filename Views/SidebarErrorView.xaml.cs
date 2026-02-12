using CommonPluginsShared.Controls;
using Playnite.SDK;
using System;
using System.Windows.Controls;

namespace Calculator
{
	public partial class SidebarErrorView : UserControl
	{
		public Exception Error { get; }

		public SidebarErrorView(Exception exception)
		{
			Error = exception;
			DataContext = this;
			InitializeComponent();
		}

		public static UserControl Create(Exception exception)
		{
			var instance = new SidebarErrorView(exception);
			var control = new SidebarItemControl();
			control.SetTitle(ResourceProvider.GetString("LOCCalculator"));
			control.AddContent(instance);

			return control;
		}

	}
}
