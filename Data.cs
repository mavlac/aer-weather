using Aer.Utils;
using Aer.Weather;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Aer
{
	/// <summary>
	/// Holds the set location and cached weather data.
	/// Able to update itself from network using the IWeatherProvider
	/// </summary>	
	public static class Data
	{
		private const float CacheValidityMinutes = 30f;

		private const string LocationLabelFormat = "{0}, {1}"; // Name, Country
		private const int DefaultLocationID = 3067696;
		private const string DefaultLocationName = "Prague";
		private const string DefaultLocationCountry = "CZ";
		private const double DefaultLocationLatitude = 50.08804;
		private const double DefaultLocationLongitude = 14.42076;

		private static string SettingsPrefix => nameof(Data);

		private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

		private static bool IsUpdatingFromNetwork { get; set; }

		public static int? LocationID { get; private set; }
		public static string? LocationLabel { get; private set; }
		public static double? LocationLatitude { get; private set; }
		public static double? LocationLongitude { get; private set; }

		public static int? CacheLocationID { get; private set; }
		public static int? CachedConditionCode { get; private set; }
		public static bool? CachedIsDaytime { get; private set; }
		public static double? CachedTemperature { get; private set; } // Stored in Celsius. Converted if shown as Fahrenheit
		public static List<HourlyForecast> CachedHourly { get; private set; } = new();
		public static DateTime? CacheLastUpdateTime { get; private set; }
		public static bool IsCacheDataValid { get; private set; }

		public static string? LocationCoordinates => LocationLatitude is null || LocationLongitude is null ? null : $"{LocationLatitude.Value.ToString(CultureInfo.InvariantCulture)}, {LocationLongitude.Value.ToString(CultureInfo.InvariantCulture)}";
		public static string ReadableTemperature => CachedTemperature is null ? "—" : Temperature.GetReadableTemperature(CachedTemperature.Value);
		public static string ConditionDescription => CachedConditionCode is null ? "—" : WeatherDescriptions.GetDescription(CachedConditionCode.Value, CachedIsDaytime!.Value);
		public static string ConditionWeatherIconsGlyph => CachedConditionCode is null || CachedIsDaytime is null ? WeatherIconsUtils.Unknown : WeatherIconsUtils.GetWeatherIcon(CachedConditionCode!.Value, CachedIsDaytime!.Value);

		public static void LoadCacheOrDefaults()
		{
			// Will load location, and if loaded and matching the location of cached data, will load cache.
			// If location is not valid, will load default location and invalidate cached data flag IsCacheDataValid

			var settings = ApplicationData.Current.LocalSettings;
			bool isSavedLocationLoadSuccessful;
			bool isCachedDataMatchingLocation;

			// Loading the saved location details
			if (settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(LocationID)}", out var locationIDObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(LocationLabel)}", out var locationLabelObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(LocationLatitude)}", out var locationLatitudeObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(LocationLongitude)}", out var locationLongitudeObj))
			{
				LocationID = (int)locationIDObj;
				LocationLabel = (string)locationLabelObj;
				LocationLatitude = (double)locationLatitudeObj;
				LocationLongitude = (double)locationLongitudeObj;
				isSavedLocationLoadSuccessful = true;
			}
			else
			{
				SetLocation(DefaultLocationID, DefaultLocationName, DefaultLocationCountry, DefaultLocationLatitude, DefaultLocationLongitude);
				isSavedLocationLoadSuccessful = false;
			}

			// Getting the cached location to compare with loaded location (cache can be fine, but location changed meanwhile)
			if (settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(CacheLocationID)}", out var cacheLocationIDObj))
			{
				CacheLocationID = (int)cacheLocationIDObj;
				isCachedDataMatchingLocation = CacheLocationID == LocationID;
			}
			else
			{
				isCachedDataMatchingLocation = false;
			}

			// Loading the saved weather data, only if the location was loaded successfully and cache location matches
			if (isSavedLocationLoadSuccessful &&
				isCachedDataMatchingLocation &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(CachedConditionCode)}", out var cachedConditionCodeObj) && cachedConditionCodeObj is int conditionCode &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(CachedIsDaytime)}", out var cachedIsDaytimeObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(CachedTemperature)}", out var cachedTemperatureObj) && cachedTemperatureObj is double temperature &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(CachedHourly)}", out var cachedHourlyObj) && JsonSerializer.Deserialize<List<HourlyForecast>>((string)cachedHourlyObj, _jsonOptions) is List<HourlyForecast> hourly &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(CacheLastUpdateTime)}", out var cacheLastUpdateTimeObj) && DateTime.TryParse(cacheLastUpdateTimeObj as string, null, DateTimeStyles.RoundtripKind, out DateTime lastUpdateDateTime))
			{
				CachedConditionCode = conditionCode;
				CachedIsDaytime = (bool)cachedIsDaytimeObj;
				CachedTemperature = temperature;
				CachedHourly = hourly;
				CacheLastUpdateTime = lastUpdateDateTime;
				IsCacheDataValid = true;
			}
			else
			{
				// Location set to defaults
				// or cached data from another location
				// or no success loading the cache
				CacheLastUpdateTime = null;
				IsCacheDataValid = false;
			}

			Debug.WriteLineIf(IsCacheDataValid, $"Cache loaded and valid. All OK.");
			Debug.WriteLineIf(!IsCacheDataValid, $"Loading from the cache failed. isSavedLocationLoadSuccessful = {isSavedLocationLoadSuccessful}, isCachedDataMatchingLocation = {isCachedDataMatchingLocation}");
			Debug.WriteLineIf(!isCachedDataMatchingLocation, $"Cache location not matching selected location. '{CacheLocationID}' vs '{LocationID}'");
		}

		public static void SetLocation(int id, string newLocationName, string newLocationCountry, double newLocationLatitude, double newLocationLongitude)
		{
			LocationID = id;
			LocationLabel = string.Format(LocationLabelFormat, newLocationName, newLocationCountry);
			LocationLatitude = newLocationLatitude;
			LocationLongitude = newLocationLongitude;

			var settings = ApplicationData.Current.LocalSettings;

			settings.Values[$"{SettingsPrefix}_{nameof(LocationID)}"] = LocationID;
			settings.Values[$"{SettingsPrefix}_{nameof(LocationLabel)}"] = LocationLabel;
			settings.Values[$"{SettingsPrefix}_{nameof(LocationLatitude)}"] = LocationLatitude;
			settings.Values[$"{SettingsPrefix}_{nameof(LocationLongitude)}"] = LocationLongitude;

			IsCacheDataValid = false; // New data must be loaded always after location change
		}

		public static async Task<bool> UpdateWeatherDataFromNetwork(bool skipCache, CancellationToken cancellationToken)
		{
			Debug.Assert(IsUpdatingFromNetwork is false, "UpdateWeatherDataFromNetwork called while another update is in progress.");

			if (IsCacheDataValid && IsCachedDataRecentEnough() && !skipCache)
			{
				return true; // OK: no network update performed, because wasn't needed
			}

			IsUpdatingFromNetwork = true;

			try
			{
				cancellationToken.ThrowIfCancellationRequested();

				var provider = new Weather.OpenMeteo.OpenMeteoWeatherProvider();
				var result = await provider.GetWeatherAsync(LocationLatitude!.Value, LocationLongitude!.Value, cancellationToken);

				cancellationToken.ThrowIfCancellationRequested();

				if (result == null)
				{
					return false; // ERROR: network error or something went wrong
				}

				// On success
				CacheLocationID = LocationID;
				CachedConditionCode = result.Current.ConditionCode;
				CachedIsDaytime = result.Current.IsDaytime;
				CachedTemperature = result.Current.Temperature;
				CachedHourly = result.Hourly;
				CacheLastUpdateTime = DateTime.Now;

				IsUpdatingFromNetwork = false;
				IsCacheDataValid = true;
				SaveWeatherData();

				return true; // OK: data was updated from network
			}
			catch (OperationCanceledException)
			{
				Debug.WriteLine("Weather data update canceled.");
				return true; // OK: was canceled, but no error
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Weather update failed: {ex}");
				return false; // ERROR: failed with unexpected exception
			}
			finally
			{
				IsUpdatingFromNetwork = false;
			}
		}


		private static void SaveWeatherData()
		{
			Debug.Assert(IsUpdatingFromNetwork is false, "SaveWeatherData called while an update is in progress.");
			Debug.Assert(IsCacheDataValid, "SaveWeatherData called while weather data is not valid.");

			var settings = ApplicationData.Current.LocalSettings;

			settings.Values[$"{SettingsPrefix}_{nameof(CachedConditionCode)}"] = CachedConditionCode;
			settings.Values[$"{SettingsPrefix}_{nameof(CachedIsDaytime)}"] = CachedIsDaytime;
			settings.Values[$"{SettingsPrefix}_{nameof(CachedTemperature)}"] = CachedTemperature;
			settings.Values[$"{SettingsPrefix}_{nameof(CachedHourly)}"] = JsonSerializer.Serialize(CachedHourly, _jsonOptions); // TODO: Will not be enough for data larger than 8kB, now hourly a single day only
			settings.Values[$"{SettingsPrefix}_{nameof(CacheLocationID)}"] = CacheLocationID;
			settings.Values[$"{SettingsPrefix}_{nameof(CacheLastUpdateTime)}"] = CacheLastUpdateTime?.ToString("o");
		}

		private static bool IsCachedDataRecentEnough()
		{
			if (CacheLastUpdateTime is not null)
			{
				var timeSinceLastUpdate = DateTime.Now - CacheLastUpdateTime.Value;
				if (timeSinceLastUpdate.TotalMinutes <= CacheValidityMinutes)
					return true;
			}
			return false;
		}
	}
}
