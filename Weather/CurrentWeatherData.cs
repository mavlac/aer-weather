namespace Aer.Weather
{
	public record CurrentWeatherData
	{
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
	}
}
