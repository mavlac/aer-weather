namespace Aer.Weather.YrNo
{
	public partial class YrNoWeatherProvider
	{
		private static int GetWMOCodeFromYrNoSymbol(string? symbol)
		{
			if (string.IsNullOrEmpty(symbol))
				return 0;
			
			// TODO: refine mapping later
			if (symbol.StartsWith("clearsky"))
				return 0;
			if (symbol.Contains("cloudy"))
				return 1;
			if (symbol.Contains("rain"))
				return 2;
			if (symbol.Contains("snow"))
				return 3;
			
			return 99;
		}

		private static bool GetIsDaytimeFromYrNoSymbol(string? symbol)
		{
			if (string.IsNullOrEmpty(symbol))
				return true;
			
			return symbol.EndsWith("_day");
		}
	}
}
