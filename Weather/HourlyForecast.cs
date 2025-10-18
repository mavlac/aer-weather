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
		public double Temperature { get; set; } // Stored in Celsius. Converted if shown as Fahrenheit
		public int ConditionCode { get; set; }
		public double Rain { get; set; }
		public double Snowfall { get; set; }

		public override string ToString()
		{
			CultureInfo culture = CultureInfo.CurrentCulture;

			var formattedTime = Time.ToString(culture.DateTimeFormat.ShortDatePattern) + " " + Time.ToString("HH:mm");
			return $"{formattedTime} {WeatherDescriptions.GetDescription(ConditionCode, IsDaytime)} {TemperatureUtils.GetReadableTemperature(Temperature)} {Rain}mm/{Snowfall}mm";
		}
	}
}
