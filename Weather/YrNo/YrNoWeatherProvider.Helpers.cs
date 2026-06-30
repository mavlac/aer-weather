namespace Aer.Weather.YrNo
{
	public partial class YrNoWeatherProvider
	{
		private static int GetWMOCodeFromYrNoSymbol(string? symbol)
		{
			if (string.IsNullOrEmpty(symbol))
				return 0;

			symbol = symbol.ToLowerInvariant();

			// Clear sky
			if (symbol.StartsWith("clearsky"))
				return 0;

			// Mainly clear / fair
			if (symbol.StartsWith("fair"))
				return 1;

			// Partly cloudy
			if (symbol.StartsWith("partlycloudy"))
				return 2;

			// Cloudy / overcast
			if (symbol.StartsWith("cloudy"))
				return 3;

			// Fog
			if (symbol.Contains("fog"))
				return 45;

			// Drizzle / light rain
			if (symbol.Contains("lightrain") || symbol.Contains("rainshowers") && symbol.Contains("light"))
				return 51;

			// Rain
			if (symbol.Contains("rainshowers"))
				return 53;

			if (symbol.Contains("rain"))
				return 61;

			// Heavy rain
			if (symbol.Contains("heavyrain") || symbol.Contains("extremerain"))
				return 63;

			// Sleet / freezing rain
			if (symbol.Contains("sleet") || symbol.Contains("rainandsnow"))
				return 66;

			// Snow showers (light/moderate)
			if (symbol.Contains("snowshowers"))
				return 73;

			// Snow
			if (symbol.Contains("snow"))
				return 71;

			// Thunderstorm
			if (symbol.Contains("thunder"))
				return 95;

			// Default unknown
			return 3; // fallback: cloudy is safest neutral UI state
		}

		private static bool GetIsDaytimeFromYrNoSymbol(string? symbol)
		{
			if (string.IsNullOrEmpty(symbol))
				return true;
			
			return symbol.EndsWith("_day");
		}
	}
}
