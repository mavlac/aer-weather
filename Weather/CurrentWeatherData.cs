namespace Aer.Weather
{
	public record CurrentWeatherData
	{
		public bool IsDaytime { get; set; }
		public double Temperature { get; set; } // The value is stored in Celsius and converted if needed on display
		public int ConditionCode { get; set; } // World Meteorological Organization standard
	}
}
