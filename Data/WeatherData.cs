using Aer.Weather;
using System;
using System.Collections.Generic;

namespace Aer.Data
{
	/// <summary>
	/// The single Data record that contains all the weather data for a specific location and weather provider
	/// </summary>
	internal record WeatherData
	{
		public int locationID;
		public int weatherProviderID;
		
		public required CurrentWeather currentWeather;
		public required List<HourlyForecast> hourlyForecast;
		
		public DateTimeOffset validUntil;
	}
}
