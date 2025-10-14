using System.Collections.Generic;

namespace Aer.Weather
{
	public class WeatherResult
	{
		public WeatherData Current { get; set; } = new();
		public List<HourlyForecast> HourlyForecast { get; set; } = new();
	}
}
