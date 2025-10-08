using Aer.Utils;
using Microsoft.UI.Xaml;
using System.Diagnostics;
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
		public static ElementTheme AppTheme { get; private set; } = ElementTheme.Default;

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

			if (settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(AppTheme)}", out var appThemeObj) && appThemeObj is int appThemeIntValue)
			{
				AppTheme = (ElementTheme)appThemeIntValue;
			}
			else
			{
				AppTheme = ElementTheme.Default;
			}
		}

		public static void Save()
		{
			var settings = ApplicationData.Current.LocalSettings;

			settings.Values[$"{SettingsPrefix}_{nameof(TemperatureUnits)}"] = (int)TemperatureUnits;
			settings.Values[$"{SettingsPrefix}_{nameof(AppTheme)}"] = (int)AppTheme;
		}

		public static void SetTemperatureUnits(TemperatureUnit unit)
		{
			Debug.WriteLine($"Preferences: Setting temperature units to {unit}");
			TemperatureUnits = unit;
			Save();
		}

		public static void SetAppTheme(ElementTheme newTheme)
		{
			AppTheme = newTheme;
			Save();
		}
	}
}
