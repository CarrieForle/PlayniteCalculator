namespace Calculator
{
	public class PlaytimeHelper
	{
		public enum PlaytimeDisplayFormat
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

		public static string PlaytimeToString(PlaytimeDisplayFormat mode, bool paddingZero, ulong seconds)
		{
			ulong minutes = seconds / 60;
			ulong hours = seconds / 3600;
			ulong days = seconds / 86400;

			switch (mode)
			{
				case PlaytimeDisplayFormat.Second:
					return $"{seconds}s";
				case PlaytimeDisplayFormat.Minute:
					return $"{minutes}m";
				case PlaytimeDisplayFormat.MinuteSecond:
					seconds %= 60;
					return paddingZero ? $"{minutes}m {seconds}s" : $"{minutes}m {seconds:D2}s";
				case PlaytimeDisplayFormat.Hour:
					return $"{hours}h";
				case PlaytimeDisplayFormat.HourMinute:
					minutes %= 60;
					return paddingZero ? $"{hours}h {minutes}m" : $"{hours}h {minutes:D2}m";
				case PlaytimeDisplayFormat.HourMinuteSecond:
					minutes %= 60;
					seconds %= 60;
					return paddingZero ? $"{hours}h {minutes}m {seconds}s" : $"{hours}h {minutes:D2}m {seconds:D2}s";
				case PlaytimeDisplayFormat.Day:
					return $"{days}d";
				case PlaytimeDisplayFormat.DayHour:
					hours %= 24;
					return paddingZero ? $"{days}d {hours}h" : $"{days}d {hours:D2}h";
				case PlaytimeDisplayFormat.DayHourMinute:
					hours %= 24;
					minutes %= 60;
					return paddingZero ? $"{days}d {hours}h {minutes}m" : $"{days}d {hours:D2}h {minutes:D2}m";
				case PlaytimeDisplayFormat.DayHourMinuteSecond:
				default:
					hours %= 24;
					minutes %= 60;
					seconds %= 60;
					return paddingZero ? $"{days}d {hours}h {minutes}m {seconds}s" : $"{days}d {hours:D2}h {minutes:D2}m {seconds:D2}s";
			}
		}
	}
}
