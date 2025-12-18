using Aer.Data;
using System;
using Windows.Globalization;

namespace Aer.Utils
{
	public static class TemperatureUtils
	{
		public enum Unit
		{
			Celsius,
			Fahrenheit
		}

		/// <summary>
		/// Returns a readable temperature string in the user's preferred units, including unit symbol.
		/// </summary>
		public static string GetReadableTemperature(double celsiusTemperature, string separator)
		{
			double t = GetTemperatureInPreferredUnit(celsiusTemperature);
			int value = (int)Math.Round(t); // Eliminates -0 automatically

			return $"{value}{separator}{Preferences.TemperatureUnits.UnitString()}";
		}

		public static double GetTemperatureInPreferredUnit(double celsiusTemperature)
		{
			return Preferences.TemperatureUnits == Unit.Celsius
				? celsiusTemperature
				: CelsiusToFahrenheit(celsiusTemperature);
		}

		public static double CelsiusToFahrenheit(double celsiusTemperature)
		{
			return (celsiusTemperature * 9.0 / 5.0) + 32.0;
		}

		public static Unit GetPreferredTemperatureUnit()
		{
			var region = new GeographicRegion();
			var code = region.CodeTwoLetter; // e.g. "US", "GB", "DE"
			
			// Fahrenheit countries
			string[] fahrenheitRegions = ["US", "BS", "BZ", "KY", "PW"];
			
			if (Array.Exists(fahrenheitRegions, r => r == code))
			{
				return Unit.Fahrenheit;
			}
			
			return Unit.Celsius;
		}



		// Enum extension
		public static string UnitString(this Unit unit)
		{
			return unit switch
			{
				Unit.Celsius => "°C",
				Unit.Fahrenheit => "°F",
				_ => throw new NotImplementedException()
			};
		}
	}
}
