using System;

namespace Aer.Utils
{
	public static class DateTimeUtils
	{
		public static string GetRelativeTimeString(DateTime? dateTime)
		{
			if (dateTime is null)
				return "never";

			var now = DateTime.Now;
			var ts = now - dateTime.Value;

			if (ts.TotalSeconds < 60)
			{
				return "just now";
			}

			if (ts.TotalMinutes < 60)
			{
				int minutes = (int)ts.TotalMinutes;
				return $"{minutes} minute{(minutes > 1 ? "s" : string.Empty)} ago";
			}

			if (ts.TotalHours < 24)
			{
				int hours = (int)ts.TotalHours;
				return $"{hours} hour{(hours > 1 ? "s" : string.Empty)} ago";
			}

			if (ts.TotalDays < 2)
				return "yesterday";

			if (ts.TotalDays < 7)
				return $"{(int)ts.TotalDays} days ago";

			return dateTime.Value.ToString("MMM d");
		}
	}
}
