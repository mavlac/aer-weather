namespace Aer.Weather
{
	public record CurrentWeatherData
	{
		public bool IsDaytime { get; set; }
		public double Temperature { get; set; } // Stored in Celsius. Converted if shown as Fahrenheit
		public int ConditionCode { get; set; }
		public string ObservationTime { get; set; } = string.Empty;
	}
}
