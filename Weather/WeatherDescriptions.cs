using System.Collections.Generic;

namespace Aer.Weather
{
	public static class WeatherDescriptions
	{
		private static readonly Dictionary<int, (string day, string night)> CodeToDescription = new()
		{
			{ 0, ("Sunny", "Clear") },
			{ 1, ("Mainly Sunny", "Mainly Clear") },
			{ 2, ("Partly Cloudy", "Partly Cloudy") },
			{ 3, ("Cloudy", "Cloudy") },
			{ 45, ("Foggy", "Foggy") },
			{ 48, ("Rime Fog", "Rime Fog") },
			{ 51, ("Light Drizzle", "Light Drizzle") },
			{ 53, ("Drizzle", "Drizzle") },
			{ 55, ("Heavy Drizzle", "Heavy Drizzle") },
			{ 56, ("Light Freezing Drizzle", "Light Freezing Drizzle") },
			{ 57, ("Freezing Drizzle", "Freezing Drizzle") },
			{ 61, ("Light Rain", "Light Rain") },
			{ 63, ("Rain", "Rain") },
			{ 65, ("Heavy Rain", "Heavy Rain") },
			{ 66, ("Light Freezing Rain", "Light Freezing Rain") },
			{ 67, ("Freezing Rain", "Freezing Rain") },
			{ 71, ("Light Snow", "Light Snow") },
			{ 73, ("Snow", "Snow") },
			{ 75, ("Heavy Snow", "Heavy Snow") },
			{ 77, ("Snow Grains", "Snow Grains") },
			{ 80, ("Light Showers", "Light Showers") },
			{ 81, ("Showers", "Showers") },
			{ 82, ("Heavy Showers", "Heavy Showers") },
			{ 85, ("Light Snow Showers", "Light Snow Showers") },
			{ 86, ("Snow Showers", "Snow Showers") },
			{ 95, ("Thunderstorm", "Thunderstorm") },
			{ 96, ("Light Thunderstorms with Hail", "Light Thunderstorms with Hail") },
			{ 99, ("Thunderstorm with Hail", "Thunderstorm with Hail") },
		};

		public static string GetDescription(int WMOCode, bool isDaytime)
		{
			if (CodeToDescription.TryGetValue(WMOCode, out var desc))
			{
				return isDaytime ? desc.day : desc.night;
			}

			return $"Unknown (WMO {WMOCode})";
		}
	}
}
