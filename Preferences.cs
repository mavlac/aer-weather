using Aer.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Aer
{
	public static class Preferences
	{
		public enum TemperatureUnit
		{
			Celsius,
			Fahrenheit
		}

		private static string SettingsPrefix => nameof(Preferences);

		public static TemperatureUnit TemperatureUnits { get; private set; } = TemperatureUnit.Celsius;

		public static void Load()
		{
			var settings = ApplicationData.Current.LocalSettings;

			if (settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(TemperatureUnits)}", out var temperatureUnitsObj) && temperatureUnitsObj is int temperatureUnitsIntValue)
			{
				TemperatureUnits = (TemperatureUnit)temperatureUnitsIntValue;
			}
			else
			{
				TemperatureUnits = LocalizationUtils.GetPreferredTemperatureUnit();
			}
		}

		public static void Save()
		{
			var settings = ApplicationData.Current.LocalSettings;

			settings.Values[$"{SettingsPrefix}_{nameof(TemperatureUnits)}"] = (int)TemperatureUnits;
		}

		public static void SetTemperatureUnits(TemperatureUnit unit)
		{
			Debug.WriteLine($"Preferences: Setting temperature units to {unit}");
			TemperatureUnits = unit;
			Save();
		}
	}
}
