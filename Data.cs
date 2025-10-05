using System;
using System.Globalization;
using Windows.Storage;

namespace Aer
{
	public static class Data
	{
		private const string LocationLabelFormat = "{0}, {1}";
		private const double DefaultLocationLatitude = 50.08804;
		private const double DefaultLocationLongitude = 14.42076;
		private const string DefaultLastUpdateTime = "—";

		private static string DefaultLocationLabel => string.Format(LocationLabelFormat, "Prague", "CZ");

		private static string SettingsPrefix => nameof(Data);

		public static string? LocationLabel { get; private set; }
		public static double? LocationLatitude { get; private set; }
		public static double? LocationLongitude { get; private set; }

		public static bool IsWeatherDataValid { get; private set; }
		public static string? Condition { get; private set; }
		public static string? Temperature { get; private set; }
		public static string? LastUpdateTime { get; private set; }

		/// <summary>
		/// Readable Latitude and Longitude. Both values are stored separately.
		/// </summary>
		public static string? LocationCoordinates => LocationLatitude is null || LocationLongitude is null ? null : $"{LocationLatitude.Value.ToString(CultureInfo.InvariantCulture)}, {LocationLongitude.Value.ToString(CultureInfo.InvariantCulture)}";

		public static event Action? Updated;

		public static void LoadLastSavedValues()
		{
			// Will load location and if valid, will load cached data
			// If location is not valid, will load default location and invalidate cached data

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
				LocationLabel = DefaultLocationLabel;
				LocationLatitude = DefaultLocationLatitude;
				LocationLongitude = DefaultLocationLongitude;
			}

			// Loading the saved weather data, only if the location was loaded successfully
			if (isSavedLocationLoadSuccessful &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(Condition)}", out var conditionNameObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(Temperature)}", out var temperatureNameObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(LastUpdateTime)}", out var lastUpdateTimeObj))
			{
				Condition = (string)conditionNameObj;
				Temperature = (string)temperatureNameObj;
				LastUpdateTime = (string)lastUpdateTimeObj;
				IsWeatherDataValid = true;
			}
			else
			{
				LastUpdateTime = DefaultLastUpdateTime;
				IsWeatherDataValid = false;
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

			IsWeatherDataValid = false; // New data must be loaded
		}

		public static void UpdateWeatherDataFromNetwork()
		{
			// TODO: implement loading from data provider

			// TODO: On Success
			//IsWeatherDataValid = true;
			//Updated?.Invoke();

			//SaveWeatherData();
		}

		private static void SaveWeatherData()
		{
			if (!IsWeatherDataValid) throw new InvalidOperationException("Data must be loaded before it can be saved.");

			var settings = ApplicationData.Current.LocalSettings;

			settings.Values[$"{SettingsPrefix}_{nameof(Condition)}"] = Condition;
			settings.Values[$"{SettingsPrefix}_{nameof(Temperature)}"] = Temperature;
			settings.Values[$"{SettingsPrefix}_{nameof(LastUpdateTime)}"] = LastUpdateTime;
		}
	}
}
