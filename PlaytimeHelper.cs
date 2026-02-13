namespace Calculator
{
	using System.Text;
	using static PlaytimeHelper.PlaytimeDisplayFormat;

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
				case Second:
					return $"{seconds}s";
				case Minute:
					return $"{minutes}m";
				case Hour:
					return $"{hours}h";
				case Day:
					return $"{days}d";
			}

			string Format(ulong? d, ulong? h, ulong? m, ulong? s)
			{
				var sb = new StringBuilder();

				bool startPadding = false;

				if (!(d is null) && d > 0)
				{
					sb.Append($" {d:D2}d");
					startPadding = paddingZero;
				}

				if (!(h is null) && h > 0)
				{
					if (startPadding)
					{
						sb.Append($" {h:D2}h");
					}
					else
					{
						sb.Append($" {h}h");
						startPadding = paddingZero;
					}
				}

				if (!(m is null) && m > 0)
				{
					if (startPadding)
					{
						sb.Append($" {m:D2}m");
					}
					else
					{
						sb.Append($" {m}m");
						startPadding = paddingZero;
					}
				}

				if (!(s is null) && s > 0)
				{
					if (startPadding)
					{
						sb.Append($" {s:D2}s");
					}
					else
					{
						sb.Append($" {s}s");
					}
				}

				return sb.ToString().Trim();
			}

			switch (mode)
			{
				case MinuteSecond:
					seconds %= 60;
					return Format(null, null, minutes, seconds);
				case HourMinute:
					minutes %= 60;
					return Format(null, hours, minutes, null);
				case HourMinuteSecond:
					minutes %= 60;
					seconds %= 60;
					return Format(null, hours, minutes, seconds);
				case DayHour:
					hours %= 24;
					return Format(days, hours, null, null);
				case DayHourMinute:
					hours %= 24;
					minutes %= 60;
					return Format(days, hours, minutes, null);
				case DayHourMinuteSecond:
				default:
					hours %= 24;
					minutes %= 60;
					seconds %= 60;
					return Format(days, hours, minutes, seconds);
			}
		}
	}
}
