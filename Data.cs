using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Storage;

namespace Aer
{
	public static class Data
	{
		private const float CacheValidityMinutes = 30f;

		private const string LocationLabelFormat = "{0}, {1}"; // Name, Country
		private const string DefaultLocationName = "Prague";
		private const string DefaultLocationCountry = "CZ";
		private const double DefaultLocationLatitude = 50.08804;
		private const double DefaultLocationLongitude = 14.42076;

		private static string SettingsPrefix => nameof(Data);

		private static bool isUpdatingFromNetwork;

		public static string? LocationLabel { get; private set; }
		public static double? LocationLatitude { get; private set; }
		public static double? LocationLongitude { get; private set; }

		public static bool IsWeatherDataLoaded { get; private set; }
		public static string? Condition { get; private set; }
		public static double? Temperature { get; private set; } // Stored in Celsius. Converted if shown as Fahrenheit
		public static DateTime? LastUpdateTime { get; private set; }

		/// <summary>
		/// Readable Latitude and Longitude. Both values are stored separately.
		/// </summary>
		public static string? LocationCoordinates => LocationLatitude is null || LocationLongitude is null ? null : $"{LocationLatitude.Value.ToString(CultureInfo.InvariantCulture)}, {LocationLongitude.Value.ToString(CultureInfo.InvariantCulture)}";
		/// <summary>
		/// Readable Temperature - value with units based on stored measurement preference.
		/// </summary>
		public static string ReadableTemperature => Preferences.TemperatureUnits == Preferences.TemperatureUnit.Celsius ? ReadableTemperatureCelsius : ReadableTemperatureFahrenheit;
		public static string ReadableTemperatureCelsius => Temperature is null ? "—" : $"{Math.Round(Temperature.Value)} {Preferences.TemperatureUnit.Celsius.ToUnitString()}";
		public static string ReadableTemperatureFahrenheit => Temperature is null ? "—" : $"{Math.Round(Temperature.Value * 1.8d + 32)} {Preferences.TemperatureUnit.Fahrenheit.ToUnitString()}";

		public static event Action? UpdatedFromNetwork;

		public static void LoadCacheOrDefaults()
		{
			// Will load location, and if valid, will load cached data.
			// If location is not valid, will load default location and invalidate cached data flag IsWeatherDataLoaded

			var settings = ApplicationData.Current.LocalSettings;
			var isSavedLocationLoadSuccessful = false;

			// Loading the saved location
			if (settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(LocationLabel)}", out var locationLabelObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(LocationLatitude)}", out var locationLatitudeObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(LocationLongitude)}", out var locationLongitudeObj))
			{
				LocationLabel = (string)locationLabelObj;
				LocationLatitude = (double)locationLatitudeObj;
				LocationLongitude = (double)locationLongitudeObj;
				isSavedLocationLoadSuccessful = true;
			}
			else
			{
				SetLocation(DefaultLocationName, DefaultLocationCountry, DefaultLocationLatitude, DefaultLocationLongitude);
			}

			// Loading the saved weather data, only if the location was loaded successfully
			if (isSavedLocationLoadSuccessful &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(Condition)}", out var conditionObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(Temperature)}", out var temperatureObj) && temperatureObj is double temperature &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(LastUpdateTime)}", out var lastUpdateTimeObj) && DateTime.TryParse(lastUpdateTimeObj as string, null, DateTimeStyles.RoundtripKind, out DateTime lastUpdateDateTime))
			{
				Condition = (string)conditionObj;
				Temperature = temperature;
				LastUpdateTime = lastUpdateDateTime;
				IsWeatherDataLoaded = true;
			}
			else
			{
				// Location set to defaults, or no success loading the cache
				LastUpdateTime = null;
				IsWeatherDataLoaded = false;
			}
		}

		public static void SetLocation(string newLocationName, string newLocationCountry, double newLocationLatitude, double newLocationLongitude)
		{
			LocationLabel = string.Format(LocationLabelFormat, newLocationName, newLocationCountry);
			LocationLatitude = newLocationLatitude;
			LocationLongitude = newLocationLongitude;

			var settings = ApplicationData.Current.LocalSettings;

			settings.Values[$"{SettingsPrefix}_{nameof(LocationLabel)}"] = LocationLabel;
			settings.Values[$"{SettingsPrefix}_{nameof(LocationLatitude)}"] = LocationLatitude;
			settings.Values[$"{SettingsPrefix}_{nameof(LocationLongitude)}"] = LocationLongitude;

			IsWeatherDataLoaded = false; // New data must be loaded
		}

		public async static Task<bool> UpdateWeatherDataFromNetwork(bool skipCache)
		{
			Debug.Assert(isUpdatingFromNetwork is false, "UpdateWeatherDataFromNetwork called while another update is in progress.");

			if (IsCacheValid() && !skipCache)
			{
				IsWeatherDataLoaded = true;
				return false; // means: no network update performed
			}

			isUpdatingFromNetwork = true;

			// TODO: implement loading from data provider

			await Task.Delay(1000); // Simulate network delay

			// On success
			Condition = "Light Showers";
			Temperature = 10.758431d;
			LastUpdateTime = DateTime.Now;
			IsWeatherDataLoaded = true;
			isUpdatingFromNetwork = false;
			SaveWeatherData();
			UpdatedFromNetwork?.Invoke();
			return true; // means: data was updated from network
		}

		private static bool IsCacheValid()
		{
			if (LastUpdateTime is not null)
			{
				var timeSinceLastUpdate = DateTime.Now - LastUpdateTime.Value;
				if (timeSinceLastUpdate.TotalMinutes <= CacheValidityMinutes)
					return true;
			}
			return false;
		}

		private static void SaveWeatherData()
		{
			if (!IsWeatherDataLoaded || isUpdatingFromNetwork)
				throw new InvalidOperationException("Data must be loaded and no update should be running, before data can be saved.");

			var settings = ApplicationData.Current.LocalSettings;

			settings.Values[$"{SettingsPrefix}_{nameof(Condition)}"] = Condition;
			settings.Values[$"{SettingsPrefix}_{nameof(Temperature)}"] = Temperature;
			settings.Values[$"{SettingsPrefix}_{nameof(LastUpdateTime)}"] = LastUpdateTime?.ToString("o");
		}
	}
}
