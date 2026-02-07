using System;
using System.Windows.Controls;

namespace Calculator
{
    public partial class SidebarErrorView : UserControl
    {
        public Exception exception { get; }

        public SidebarErrorView(Exception exception = null)
        {
            this.exception = exception;
            InitializeComponent();
        }
    }
}
