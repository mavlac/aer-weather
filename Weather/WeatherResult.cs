using System;
using System.Collections.Generic;

namespace Aer.Weather
{
	public record WeatherResult
	{
		public CurrentWeather Current { get; private set; } = new();
		public List<HourlyForecast> Hourly { get; private set; } = new();
		public DateTimeOffset ValidUntil { get; private set; }

		public WeatherResult(CurrentWeather current, List<HourlyForecast> hourly, DateTimeOffset validUntil)
		{
			Current = current;
			Hourly = hourly;
			ValidUntil = validUntil;
		}
	}
}
