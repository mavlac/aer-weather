namespace Aer.Utils
{
	public static class WeatherIconsUtils
	{
		public static string GetWeatherIcon(int WMOCode, bool isDaytime)
		{
			return (WMOCode, isDaytime) switch
			{
				// Day time
				(0, true) => "&#xf00d;", // Sunny
				
				// Night time
				(0, false) => "&#xf02e;", // Sunny
				
				// Unknown
				_ => "?"
			};
		}
	}
}
