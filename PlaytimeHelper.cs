namespace Calculator
{
	public class PlaytimeHelper
	{
		public enum PlaytimeDisplayMode
		{
			Second,
			Minute,
			MinuteSecond,
			Hour,
			HourMinute,
			HourMinuteSecond,
			Day,
			DayHour,
			DayHourMinute,
			DayHourMinuteSecond,
		}

		public static string PlaytimeToString(PlaytimeDisplayMode mode, bool paddingZero, ulong seconds)
		{
			ulong minutes = seconds / 60;
			ulong hours = seconds / 3600;
			ulong days = seconds / 86400;

			switch (mode)
			{
				case PlaytimeDisplayMode.Second:
					return $"{seconds}s";
				case PlaytimeDisplayMode.Minute:
					return $"{minutes}m";
				case PlaytimeDisplayMode.MinuteSecond:
					seconds %= 60;
					return paddingZero ? $"{minutes}m {seconds}s" : $"{minutes}m {seconds:D2}s";
				case PlaytimeDisplayMode.Hour:
					return $"{hours}h";
				case PlaytimeDisplayMode.HourMinute:
					minutes %= 60;
					return paddingZero ? $"{hours}h {minutes}m" : $"{hours}h {minutes:D2}m";
				case PlaytimeDisplayMode.HourMinuteSecond:
					minutes %= 60;
					seconds %= 60;
					return paddingZero ? $"{hours}h {minutes}m {seconds}s" : $"{hours}h {minutes:D2}m {seconds:D2}s";
				case PlaytimeDisplayMode.Day:
					return $"{days}d";
				case PlaytimeDisplayMode.DayHour:
					hours %= 24;
					return paddingZero ? $"{days}d {hours}h" : $"{days}d {hours:D2}h";
				case PlaytimeDisplayMode.DayHourMinute:
					hours %= 24;
					minutes %= 60;
					return paddingZero ? $"{days}d {hours}h {minutes}m" : $"{days}d {hours:D2}h {minutes:D2}m";
				case PlaytimeDisplayMode.DayHourMinuteSecond:
				default:
					hours %= 24;
					minutes %= 60;
					seconds %= 60;
					return paddingZero ? $"{days}d {hours}h {minutes}m {seconds}s" : $"{days}d {hours:D2}h {minutes:D2}m {seconds:D2}s";
			}
		}
	}
}
