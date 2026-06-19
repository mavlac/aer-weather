using Aer.Utils;
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
		public int LocationID { get; init; }
		public int WeatherProviderID { get; init; }

		public DateTimeOffset Created { get; init; }
		public DateTimeOffset ValidUntil { get; init; }

		public required CurrentWeather CurrentWeather { get; init; }
		public required List<HourlyForecast> HourlyForecast { get; init; }

		public string ReadableTemperature => TemperatureUtils.GetReadableTemperature(CurrentWeather.Temperature, " ", 0);
		public string ReadableApparentTemperature => TemperatureUtils.GetReadableTemperature(CurrentWeather.ApparentTemperature, " ", 0);
		public string ConditionDescription => WeatherDescriptions.GetDescription(CurrentWeather.WeatherCode, CurrentWeather.IsDaytime);
		public string ConditionWeatherIconsGlyph => WeatherIconsUtils.GetWeatherIcon(CurrentWeather.WeatherCode, CurrentWeather.IsDaytime);
	}
}
