using Aer.Utils;
using System;
using System.Globalization;

namespace Aer.Weather
{
	public record HourlyForecast
	{
		/// <summary>
		/// The time of the forecasted hour.
		/// UTC / GMT+0
		/// </summary>
		public DateTimeOffset Time { get; set; }
		/// <summary>
		/// True if the hour is during daytime, false if nighttime.
		/// This is used to determine which weather icon to show for the hour.
		/// </summary>
		public bool IsDaytime { get; set; }
		/// <summary>
		/// Stored in Celsius. If shown as Fahrenheit to the user, conversion is done on display.
		/// </summary>
		public double Temperature { get; set; }
		/// <summary>
		/// Stored in Celsius. If shown as Fahrenheit to the user, conversion is done on display.
		/// </summary>
		public double ApparentTemperature { get; set; }
		/// <summary>
		/// WMO Code - World Meteorological Organization standard
		/// </summary>
		public int WeatherCode { get; set; }
		public double Rain { get; set; }
		public double Snowfall { get; set; }

		public DateTime LocalTime => Time.ToLocalTime().DateTime;

		public override string ToString()
		{
			CultureInfo culture = CultureInfo.CurrentCulture;
			
			const string separator = " ";
			string shortDate = LocalTime.ToString(culture.DateTimeFormat.ShortDatePattern);
			string shortLocalTime = LocalTime.ToString(culture.DateTimeFormat.ShortTimePattern);
			string utcTime = Time.ToString("u");
			string temperature = TemperatureUtils.GetReadableTemperature(Temperature, string.Empty, 1);
			string apparentTemperature = TemperatureUtils.GetReadableTemperature(ApparentTemperature, string.Empty, 1);
			
			return
				$"{shortDate} {shortLocalTime} ({utcTime}){separator}" +
				$"{temperature}/{apparentTemperature}{separator}" +
				$"WMO:{WeatherCode}{separator}" +
				$"{WeatherDescriptions.GetDescription(WeatherCode, IsDaytime)}{separator}" +
				$"{Rain}mm{separator}" +
				$"{Snowfall}mm";
		}
	}
}
