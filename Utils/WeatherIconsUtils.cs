namespace Aer.Utils
{
	public static class WeatherIconsUtils
	{
		public const string Unknown = "?";

		public static string GetWeatherIcon(int WMOCode, bool isDaytime)
		{
			return WMOCode switch
			{
				// Clear / Sunny
				0 => isDaytime ? "&#xf00d;" : "&#xf02e;", // wi_day_sunny / wi_night_clear

				// Mainly Sunny / Mainly Clear
				1 => isDaytime ? "&#xf00c;" : "&#xf081;", // wi_day_sunny_overcast / wi_night_alt_partly_cloudy

				// Partly Cloudy
				2 => isDaytime ? "&#xf002;" : "&#xf081;", // wi_day_cloudy / wi_night_alt_partly_cloudy

				// Cloudy / Overcast
				3 => isDaytime ? "&#xf013;" : "&#xf031;", // wi_cloudy / wi_night_cloudy

				// Fog / Rime Fog
				45 or 48 => isDaytime ? "&#xf003;" : "&#xf04a;", // wi_day_fog / wi_night_fog

				// Drizzle
				51 or 53 or 55 => isDaytime ? "&#xf00b;" : "&#xf02b;", // wi_day_sprinkle / wi_night_alt_sprinkle

				// Freezing Drizzle
				56 or 57 => isDaytime ? "&#xf0b2;" : "&#xf0b4;", // wi_day_sleet / wi_night_alt_sleet

				// Rain
				61 or 63 or 65 => isDaytime ? "&#xf008;" : "&#xf028;", // wi_day_rain / wi_night_alt_rain

				// Freezing Rain
				66 or 67 => isDaytime ? "&#xf006;" : "&#xf026;", // wi_day_rain_mix / wi_night_alt_rain_mix

				// Snow
				71 or 73 or 75 => isDaytime ? "&#xf00a;" : "&#xf02a;", // wi_day_snow / wi_night_alt_snow

				// Snow Grains
				77 => isDaytime ? "&#xf064;" : "&#xf066;", // wi_snow_wind / wi_night_snow_wind

				// Showers
				80 or 81 or 82 => isDaytime ? "&#xf009;" : "&#xf029;", // wi_day_showers / wi_night_alt_showers

				// Snow Showers
				85 or 86 => isDaytime ? "&#xf00a;" : "&#xf02a;", // wi_day_snow / wi_night_alt_snow

				// Thunderstorms
				95 or 96 or 99 => isDaytime ? "&#xf010;" : "&#xf02d;", // wi_day_thunderstorm / wi_night_alt_thunderstorm

				// Unknown
				_ => Unknown,
			};
		}
	}
}
