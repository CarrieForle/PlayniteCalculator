using System;

namespace Calculator
{
	public class PlaytimeHelper
	{
		public static string PlaytimeToString(string format, TimeSpan ts)
		{
			return string.Format(format, ts, ts.TotalDays, ts.TotalHours, ts.TotalMinutes, (long)ts.TotalSeconds);
		}

		public static string PlaytimeToString(string format, ulong second)
		{
			// Overflow after 9.2e11 seconds,
			// or 256,204,778 hours.
			var ts = new TimeSpan((long)second * 10_000_000);
			return PlaytimeToString(format, ts);
		}
	}
}
