using System;
using Windows.Globalization;

namespace Aer.Utils
{
	internal class LocalizationUtils
	{
		public static Temperature.Unit GetPreferredTemperatureUnit()
		{
			var region = new GeographicRegion();
			var code = region.CodeTwoLetter; // e.g. "US", "GB", "DE"

			// Fahrenheit countries
			string[] fahrenheitRegions = ["US", "BS", "BZ", "KY", "PW"];

			if (Array.Exists(fahrenheitRegions, r => r == code))
			{
				return Temperature.Unit.Fahrenheit;
			}

			return Temperature.Unit.Celsius;
		}
	}
}
