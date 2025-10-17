using Aer.Utils;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using Windows.Storage;

namespace Aer
{
	public static class Preferences
	{
		private static string SettingsPrefix => nameof(Preferences);

		public static Temperature.Unit TemperatureUnits { get; private set; } = Temperature.Unit.Celsius;
		public static ElementTheme AppTheme { get; private set; } = ElementTheme.Default;
		public static bool WasWelcomeShown { get; private set; } = false;

		public static void Load()
		{
			var settings = ApplicationData.Current.LocalSettings;

			if (settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(TemperatureUnits)}", out var temperatureUnitsObj) && temperatureUnitsObj is int temperatureUnitsIntValue)
			{
				TemperatureUnits = (Temperature.Unit)temperatureUnitsIntValue;
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

			if (settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(WasWelcomeShown)}", out var wasWelcomeShownObj) && wasWelcomeShownObj is bool wasWelcomeShown)
			{
				WasWelcomeShown = wasWelcomeShown;
			}
			else
			{
				WasWelcomeShown = false;
			}
		}

		public static void Save()
		{
			var settings = ApplicationData.Current.LocalSettings;

			settings.Values[$"{SettingsPrefix}_{nameof(TemperatureUnits)}"] = (int)TemperatureUnits;
			settings.Values[$"{SettingsPrefix}_{nameof(AppTheme)}"] = (int)AppTheme;
			settings.Values[$"{SettingsPrefix}_{nameof(WasWelcomeShown)}"] = WasWelcomeShown;
		}

		public static void SetTemperatureUnits(Temperature.Unit unit)
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

		public static void SetWelcomeShown(bool shown)
		{
			WasWelcomeShown = shown;
			Save();
		}
	}
}
