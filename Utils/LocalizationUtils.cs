using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;

namespace Aer.Utils
{
	internal class LocalizationUtils
	{
		public static Preferences.TemperatureUnit GetPreferredTemperatureUnit()
		{
			var region = new GeographicRegion();
			var code = region.CodeTwoLetter; // e.g. "US", "GB", "DE"

			// Fahrenheit countries
			string[] fahrenheitRegions = ["US", "BS", "BZ", "KY", "PW"];

			if (Array.Exists(fahrenheitRegions, r => r == code))
			{
				return Preferences.TemperatureUnit.Fahrenheit;
			}

			return Preferences.TemperatureUnit.Celsius;
		}
	}
}
