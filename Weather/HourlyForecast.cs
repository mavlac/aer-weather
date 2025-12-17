using Aer.Utils;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Aer.Weather
{
	public record HourlyForecast
	{
		public DateTime Time { get; set; }
		public bool IsDaytime { get; set; }
		/// <summary>
		/// Stored in Celsius. If shown as Fahrenheit to the user, conversion is done on display.
		/// </summary>
		public double Temperature { get; set; }
		/// <summary>
		/// WMO Code - World Meteorological Organization standard
		/// </summary>
		public int WeatherCode { get; set; }
		public double Rain { get; set; }
		public double Snowfall { get; set; }

		public override string ToString()
		{
			CultureInfo culture = CultureInfo.CurrentCulture;

			const string separator = " ";
			string formattedTime = Time.ToString(culture.DateTimeFormat.ShortDatePattern) + " " + Time.ToString("HH:mm");
			string formattedTemperature = TemperatureUtils.GetReadableTemperature(Temperature, string.Empty);

			return
				$"{formattedTime}{separator}" +
				$"{formattedTemperature}{separator}" +
				$"WMO:{WeatherCode}{separator}" +
				$"{WeatherDescriptions.GetDescription(WeatherCode, IsDaytime)}{separator}" +
				$"{Rain}mm{separator}" +
				$"{Snowfall}mm";
		}
	}
}
