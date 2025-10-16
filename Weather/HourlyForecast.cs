using System;

namespace Aer.Weather
{
	public class HourlyForecast
	{
		public DateTime Time { get; set; }
		public bool IsDaytime { get; set; }
		public double Temperature { get; set; }
		public int ConditionCode { get; set; }
		public double Rain { get; set; }
		public double Snowfall { get; set; }
	}
}
