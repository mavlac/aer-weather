using System;
using System.Collections.Generic;

namespace Aer.Weather
{
	public record WeatherResult
	{
		public CurrentWeatherData Current { get; private set; } = new();
		public List<HourlyForecast> Hourly { get; private set; } = new();
		public DateTimeOffset ValidUntil { get; private set; }

		public WeatherResult(
			CurrentWeatherData current,
			List<HourlyForecast> hourly,
			DateTimeOffset validUntil)
		{
			Current = current;
			Hourly = hourly;
			ValidUntil = validUntil;
		}
	}
}
