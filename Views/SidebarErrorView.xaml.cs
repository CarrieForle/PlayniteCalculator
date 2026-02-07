using System;
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
	}
}
