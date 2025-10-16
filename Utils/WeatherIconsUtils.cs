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
				0 => isDaytime ? "\uf00d" : "\uf02e", // wi_day_sunny / wi_night_clear

				// Mainly Sunny / Mainly Clear
				1 => isDaytime ? "\uf00c" : "\uf081", // wi_day_sunny_overcast / wi_night_alt_partly_cloudy

				// Partly Cloudy
				2 => isDaytime ? "\uf002" : "\uf081", // wi_day_cloudy / wi_night_alt_partly_cloudy

				// Cloudy / Overcast
				3 => isDaytime ? "\uf013" : "\uf031", // wi_cloudy / wi_night_cloudy

				// Fog / Rime Fog
				45 or 48 => isDaytime ? "\uf003" : "\uf04a", // wi_day_fog / wi_night_fog

				// Drizzle
				51 or 53 or 55 => isDaytime ? "\uf00b" : "\uf02b", // wi_day_sprinkle / wi_night_alt_sprinkle

				// Freezing Drizzle
				56 or 57 => isDaytime ? "\uf0b2" : "\uf0b4", // wi_day_sleet / wi_night_alt_sleet

				// Rain
				61 or 63 or 65 => isDaytime ? "\uf008" : "\uf028", // wi_day_rain / wi_night_alt_rain

				// Freezing Rain
				66 or 67 => isDaytime ? "\uf006" : "\uf026", // wi_day_rain_mix / wi_night_alt_rain_mix

				// Snow
				71 or 73 or 75 => isDaytime ? "\uf00a" : "\uf02a", // wi_day_snow / wi_night_alt_snow

				// Snow Grains
				77 => isDaytime ? "\uf064" : "\uf066", // wi_snow_wind / wi_night_snow_wind

				// Showers
				80 or 81 or 82 => isDaytime ? "\uf009" : "\uf029", // wi_day_showers / wi_night_alt_showers

				// Snow Showers
				85 or 86 => isDaytime ? "\uf00a" : "\uf02a", // wi_day_snow / wi_night_alt_snow

				// Thunderstorms
				95 or 96 or 99 => isDaytime ? "\uf010" : "\uf02d", // wi_day_thunderstorm / wi_night_alt_thunderstorm

				// Unknown
				_ => Unknown,
			};
		}
	}
}
