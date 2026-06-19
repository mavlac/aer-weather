using Aer.Utils;
using Aer.Utils.Extensions;
using Aer.Weather;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using Windows.Storage;

namespace Aer.Data
{
	/// <summary>
	/// Application user preferences.
	/// Saved in LocalSettings.
	/// </summary>
	/// <remarks>
	/// The user selected location is in LocalSettings as well, but handled by <see cref="Location"/>.
	/// </remarks>
	public static class Preferences
	{
		private const string LocalSettingsKeyPrefix = nameof(Preferences);

		public static int? WeatherProviderId { get; private set; }
		public static TemperatureUtils.Unit TemperatureUnits { get; private set; }
		public static ElementTheme AppTheme { get; private set; }
		public static bool UseSystemAccentColor { get; private set; }
		public static bool UseThickChartLine { get; private set; }
		public static bool WasWelcomeShown { get; private set; }

		public static void Load()
		{
			WeatherProviderId = GetValueOrDefault(nameof(WeatherProviderId), WeatherProvider.GetPreferredProviderId());
			TemperatureUnits = (TemperatureUtils.Unit)GetValueOrDefault(nameof(TemperatureUnits), (int)TemperatureUtils.GetPreferredTemperatureUnit());
			AppTheme = (ElementTheme)GetValueOrDefault(nameof(AppTheme), (int)ElementTheme.Default);
			UseThickChartLine = GetValueOrDefault(nameof(UseThickChartLine), true);
			UseSystemAccentColor = GetValueOrDefault(nameof(UseSystemAccentColor), false);
			WasWelcomeShown = GetValueOrDefault(nameof(WasWelcomeShown), false);
			
			static T GetValueOrDefault<T>(string key, T defaultValue)
			{
				if (ApplicationData.Current.LocalSettings.Values.TryGetValue($"{LocalSettingsKeyPrefix}_{key}", out var obj) && obj is T value)
					return value;
				
				return defaultValue;
			}
		}

		public static void Save()
		{
			var localSettings = ApplicationData.Current.LocalSettings;
			
			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(WeatherProviderId)}"] = WeatherProviderId;
			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(TemperatureUnits)}"] = (int)TemperatureUnits;
			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(AppTheme)}"] = (int)AppTheme;
			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(UseSystemAccentColor)}"] = UseSystemAccentColor;
			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(UseThickChartLine)}"] = UseThickChartLine;
			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(WasWelcomeShown)}"] = WasWelcomeShown;
		}

		public static void SetWeatherProviderId(int providerId)
		{
			Debug.WriteLine($"Preferences: Setting weather provider ID to {providerId}");
			WeatherProviderId = providerId;
			Save();
		}

		public static void SetTemperatureUnits(TemperatureUtils.Unit unit)
		{
			Debug.WriteLine($"Preferences: Setting temperature units to {unit}");
			TemperatureUnits = unit;
			Save();
		}

		public static void SetAppTheme(ElementTheme newTheme)
		{
			Debug.WriteLine($"Preferences: Setting app theme to {newTheme}");
			AppTheme = newTheme;
			Save();
		}

		public static void ToggleDarkAndLightTheme()
		{
			var systemTheme = ThemeUtils.GetSystemTheme(); // Dark or Light (only. OS is always specific)

			ElementTheme newTheme;
			if (AppTheme == ElementTheme.Default)
			{
				// Is using OS default
				newTheme = systemTheme.Opposite(); // Set the opposite as override
			}
			else
			{
				// Is using specific theme override
				if (AppTheme == systemTheme)
				{
					// That is specific but matches OS default
					newTheme = systemTheme.Opposite(); // Set the opposite
				}
				else
				{
					// That is opposite to OS default
					newTheme = ElementTheme.Default; // Set the OS default
				}
			}

			SetAppTheme(newTheme);

			WindowUtils.ApplyAppTheme(App.MainWindow);
		}

		public static void SetAccentColor(bool useSystemAccentColor)
		{
			Debug.WriteLine($"Preferences: Setting accent color to {(useSystemAccentColor ? "System" : "Built in theme")}");
			UseSystemAccentColor = useSystemAccentColor;
			Save();
		}

		public static void SetLineThickness(bool useThickChartLine)
		{
			Debug.WriteLine($"Preferences: Setting line thickness to {(useThickChartLine ? "Thick" : "Thin")}");
			UseThickChartLine = useThickChartLine;
			Save();
		}

		public static void SetWelcomeShown(bool shown)
		{
			Debug.WriteLine($"Preferences: Setting welcome screen shown to {shown}");
			WasWelcomeShown = shown;
			Save();
		}
	}
}
