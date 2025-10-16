using System.Collections.Generic;

namespace Aer.Weather
{
	public class WeatherResult
	{
		public CurrentWeatherData Current { get; set; } = new();
		public List<HourlyForecast> Hourly { get; set; } = new();
	}
}
