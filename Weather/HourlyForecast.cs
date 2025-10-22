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

			const string separator = " ";
			string formattedTime = Time.ToString(culture.DateTimeFormat.ShortDatePattern) + " " + Time.ToString("HH:mm");
			
			return $"{formattedTime}{separator}{TemperatureUtils.GetReadableTemperature(Temperature)}{separator}{WeatherDescriptions.GetDescription(ConditionCode, IsDaytime)}{separator}{Rain}mm{separator}{Snowfall}mm";
		}
	}
}
