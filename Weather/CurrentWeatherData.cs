namespace Aer.Weather
{
	public record CurrentWeatherData
	{
		public bool IsDaytime { get; set; }
		public double Temperature { get; set; }
		public int ConditionCode { get; set; }
		public string ObservationTime { get; set; } = string.Empty;
	}
}
