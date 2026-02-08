using Playnite.SDK;
using System;
using System.Diagnostics;

namespace Calculator
{
	public class Commands
	{
		public static RelayCommand<Uri> NavigateUri = new RelayCommand<Uri>((uri) =>
		{
			Process.Start(new ProcessStartInfo(uri.AbsoluteUri)
			{
				UseShellExecute = true
			});
		});
	}
}
