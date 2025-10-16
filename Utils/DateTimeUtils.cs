using System;

namespace Aer.Utils
{
	public static class DateTimeUtils
	{
		/// <summary>
		/// Converts an ISO8601 timestamp (without 'Z', but known to be in UTC)
		/// to a local DateTime using the system's time zone and daylight saving rules.
		/// Example: "2025-10-16T19:45" (UTC) → "2025-10-16 21:45" (in Czechia DST)
		/// </summary>
		/// <param name="utcIsoString">ISO8601 timestamp string in UTC, without 'Z'</param>
		/// <returns>Local DateTime (DateTimeKind.Local)</returns>
		public static DateTime ConvertUtcIsoToLocal(string utcIsoString)
		{
			if (string.IsNullOrWhiteSpace(utcIsoString))
				throw new ArgumentException("Input timestamp cannot be null or empty.", nameof(utcIsoString));

			// Parse as a plain DateTime
			if (!DateTime.TryParse(utcIsoString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
				throw new FormatException($"Invalid ISO8601 timestamp format: {utcIsoString}");

			// Explicitly mark it as UTC
			var utc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

			// Convert to local time using Windows’ time zone and DST rules
			return utc.ToLocalTime();
		}

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
