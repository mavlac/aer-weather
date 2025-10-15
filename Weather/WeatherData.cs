namespace Aer.Weather
{
	public class WeatherData
	{
		public bool IsDaytime { get; set; }
		public double Temperature { get; set; }
		public int ConditionCode { get; set; }
		public string ObservationTime { get; set; } = string.Empty;

		public string ConditionText => WeatherDescriptions.GetDescription(ConditionCode);
	}
}
