using Playnite.SDK;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shell;

namespace CommonPluginsShared.Controls
{
	public partial class SidebarItemControl : UserControl
	{
		private TextBlock PART_TextBlockTitle { get; set; }
		private Grid PART_GridContener { get; set; }
		private StackPanel header { get; }

		public SidebarItemControl()
		{
			// Link
			TextBlock textBlockLink = new TextBlock
			{
				VerticalAlignment = VerticalAlignment.Center
			};
			WindowChrome.SetIsHitTestVisibleInChrome(textBlockLink, true);

			Hyperlink hyperlink = new Hyperlink();
			hyperlink.Click += (s, e) => { API.Instance.MainView.SwitchToLibraryView(); };
			hyperlink.Inlines.Add(new TextBlock
			{
				Text = "\uea5c",
				FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily,
				FontSize = 26
			});
			textBlockLink.Inlines.Add(hyperlink);

			// Title link
			PART_TextBlockTitle = new TextBlock
			{
				Style = ResourceProvider.GetResource("BaseTextBlockStyle") as Style,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(10, 0, 50, 0),
				FontSize = 18
			};

			// Link contener
			header = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Margin = new Thickness(10, 10, 0, 0)
			};
			DockPanel.SetDock(header, Dock.Top);

			header.Children.Add(textBlockLink);
			header.Children.Add(PART_TextBlockTitle);


			// Content Grid
			PART_GridContener = new Grid();

			// Content ScrollViewer
			ScrollViewer scrollViewer = new ScrollViewer
			{
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			};

			scrollViewer.Content = PART_GridContener;

			// Control
			DockPanel dockPanel = new DockPanel();
			_ = dockPanel.Children.Add(header);
			_ = dockPanel.Children.Add(scrollViewer);

			this.Content = dockPanel;
		}


		public void SetTitle(string Title)
		{
			PART_TextBlockTitle.Text = Title;
		}

		public void AddHeader(Button btn)
		{
			btn.Padding = new Thickness(btn.Padding.Left, 0, btn.Padding.Right, 0);
			btn.Margin = new Thickness(5, 0, 0, 0);
			_ = header.Children.Add(btn);
		}

		public void AddContent(FrameworkElement content)
		{
			_ = PART_GridContener.Children.Add(content);
		}
	}
}