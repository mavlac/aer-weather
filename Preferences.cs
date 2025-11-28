using Aer.Utils;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using Windows.Storage;

namespace Aer
{
	public static class Preferences
	{
		private static string SettingsPrefix => nameof(Preferences);

		// TODO: public sttic int WeatherProviderId { get; private set; } = WeatherProviderIds.OpenMeteo;

		public static TemperatureUtils.Unit TemperatureUnits { get; private set; } = TemperatureUtils.Unit.Celsius;
		public static ElementTheme AppTheme { get; private set; } = ElementTheme.Default;
		public static bool UseSystemAccentColor { get; private set; } = false;
		public static bool UseThickChartLine { get; private set; } = true;
		public static bool WasWelcomeShown { get; private set; } = false;

		public static void Load()
		{
			TemperatureUnits = (TemperatureUtils.Unit)GetValueOrDefault(nameof(TemperatureUnits), (int)LocalizationUtils.GetPreferredTemperatureUnit());
			AppTheme = (ElementTheme)GetValueOrDefault(nameof(AppTheme), (int)ElementTheme.Default);
			UseThickChartLine = GetValueOrDefault(nameof(UseThickChartLine), true);
			UseSystemAccentColor = GetValueOrDefault(nameof(UseSystemAccentColor), false);
			WasWelcomeShown = GetValueOrDefault(nameof(WasWelcomeShown), false);
			
			static T GetValueOrDefault<T>(string key, T defaultValue)
			{
				if (ApplicationData.Current.LocalSettings.Values.TryGetValue($"{SettingsPrefix}_{key}", out var obj) && obj is T value)
					return value;
				
				return defaultValue;
			}
		}

		public static void Save()
		{
			var settings = ApplicationData.Current.LocalSettings;

			settings.Values[$"{SettingsPrefix}_{nameof(TemperatureUnits)}"] = (int)TemperatureUnits;
			settings.Values[$"{SettingsPrefix}_{nameof(AppTheme)}"] = (int)AppTheme;
			settings.Values[$"{SettingsPrefix}_{nameof(UseSystemAccentColor)}"] = UseSystemAccentColor;
			settings.Values[$"{SettingsPrefix}_{nameof(UseThickChartLine)}"] = UseThickChartLine;
			settings.Values[$"{SettingsPrefix}_{nameof(WasWelcomeShown)}"] = WasWelcomeShown;
		}

		public static void SetTemperatureUnits(TemperatureUtils.Unit unit)
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

		public static void SetAccentColor(bool useSystemAccentColor)
		{
			UseSystemAccentColor = useSystemAccentColor;
			Save();
		}

		public static void SetLineThickness(bool useThickChartLine)
		{
			UseThickChartLine = useThickChartLine;
			Save();
		}

		public static void SetWelcomeShown(bool shown)
		{
			WasWelcomeShown = shown;
			Save();
		}
	}
}
