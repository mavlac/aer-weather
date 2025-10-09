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
				return "just now";
			if (ts.TotalMinutes < 60)
				return $"{(int)ts.TotalMinutes} min(s) ago";
			if (ts.TotalHours < 24)
				return $"{(int)ts.TotalHours} h(s) ago";

			if (ts.TotalDays < 2)
				return "yesterday";

			if (ts.TotalDays < 7)
				return $"{(int)ts.TotalDays} days ago";

			return dateTime.Value.ToString("MMM d");
		}
	}
}
