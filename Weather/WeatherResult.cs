using System.Collections.Generic;

namespace Aer.Weather
{
	public record WeatherResult
	{
		public CurrentWeatherData Current { get; set; } = new();
		public List<HourlyForecast> Hourly { get; set; } = new();
	}
}
