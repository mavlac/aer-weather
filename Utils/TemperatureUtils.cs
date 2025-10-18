using System;

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
		public static string GetReadableTemperature(double celsiusTemperature)
		{
			return Preferences.TemperatureUnits == Unit.Celsius
				? $"{Math.Round(celsiusTemperature)} {Unit.Celsius.UnitString()}"
				: $"{Math.Round(CelsiusToFahrenheit(celsiusTemperature))} {Unit.Fahrenheit.UnitString()}";
		}

		public static double GetTemperatureInPreferredUnit(double celsiusTemperature)
		{
			return Preferences.TemperatureUnits == Unit.Celsius
				? celsiusTemperature
				: CelsiusToFahrenheit(celsiusTemperature);
		}

		public static double CelsiusToFahrenheit(double celsius)
		{
			return (celsius * 9.0 / 5.0) + 32.0;
		}



		public static string UnitString(this Unit unit)
		{
			return unit switch
			{
				Unit.Celsius => "°C",
				Unit.Fahrenheit => "°F",
				_ => throw new System.NotImplementedException()
			};
		}
	}
}
