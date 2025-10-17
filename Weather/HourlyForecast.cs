using System;

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
	}
}
