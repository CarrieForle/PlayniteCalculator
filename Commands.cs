using Playnite.SDK;
using System;
using System.Diagnostics;

namespace Calculator
{
	public class Commands
	{
		public static RelayCommand<object> NavigateUri = new RelayCommand<object>((val) =>
		{
			if (val is Uri uri)
			{
				Process.Start(new ProcessStartInfo(uri.AbsoluteUri)
				{
					UseShellExecute = true
				});
			}
		});
	}
}
