using CommonPluginsShared.Controls;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Calculator
{
	public partial class SidebarErrorView : UserControl
	{
		public Exception Error { get; }

		public SidebarErrorView(Exception exception)
		{
			DataContext = this;
			Error = exception;
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
