using Aer.Utils;
using Aer.Weather;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
		private const string DefaultLocationName = "Prague";
		private const string DefaultLocationCountry = "CZ";
		private const double DefaultLocationLatitude = 50.08804;
		private const double DefaultLocationLongitude = 14.42076;

		private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

		private static string SettingsPrefix => nameof(Data);
		private static string AppStorageKeyPrefix => nameof(Data);

		private static bool IsUpdatingFromNetwork { get; set; }

		public static int? LocationID { get; private set; }
		public static string? LocationLabel { get; private set; }
		public static double? LocationLatitude { get; private set; }
		public static double? LocationLongitude { get; private set; }

		public static int? CacheLocationID { get; private set; }
		public static int? CacheWeatherProviderID { get; private set; }
		public static int? CachedConditionCode { get; private set; }
		public static bool? CachedIsDaytime { get; private set; }
		public static double? CachedTemperature { get; private set; } // Stored in Celsius. Converted if shown as Fahrenheit
		public static List<HourlyForecast> CachedHourly { get; private set; } = new();
		public static DateTime? CacheLastUpdateTime { get; private set; }
		public static bool IsCacheDataValid { get; private set; }

		public static string? LocationCoordinates => LocationLatitude is null || LocationLongitude is null ? null : $"{LocationLatitude.Value.ToString("F4", CultureInfo.InvariantCulture)}, {LocationLongitude.Value.ToString("F4", CultureInfo.InvariantCulture)}";
		public static string ReadableTemperature => CachedTemperature is null ? "—" : TemperatureUtils.GetReadableTemperature(CachedTemperature.Value, " ");
		public static string ConditionDescription => CachedConditionCode is null ? "—" : WeatherDescriptions.GetDescription(CachedConditionCode.Value, CachedIsDaytime!.Value);
		public static string ConditionWeatherIconsGlyph => CachedConditionCode is null || CachedIsDaytime is null ? WeatherIconsUtils.Unknown : WeatherIconsUtils.GetWeatherIcon(CachedConditionCode!.Value, CachedIsDaytime!.Value);

		public static void LoadCacheOrDefaults()
		{
			// Will load location, and if loaded and matching the location of cached data, will load cache.
			// If location is not valid, will load default location and invalidate cached data flag IsCacheDataValid

			var settings = ApplicationData.Current.LocalSettings;
			bool isSavedLocationLoadSuccessful;
			bool isCachedDataMatchingLocation;
			bool isCachedDataMatchingWeatherProvider;

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
				SetLocation(DefaultLocationName, DefaultLocationCountry, DefaultLocationLatitude, DefaultLocationLongitude);
				isSavedLocationLoadSuccessful = false;
			}

			// Getting the cached location to compare with loaded location (cache can be fine, but location changed meanwhile)
			if (AppStorage.TryLoad($"{AppStorageKeyPrefix}_{nameof(CacheLocationID)}", out int cacheLocationID))
			{
				CacheLocationID = cacheLocationID;
				isCachedDataMatchingLocation = CacheLocationID == LocationID;
			}
			else
			{
				isCachedDataMatchingLocation = false;
			}

			// Getting the cached weather provider ID to compare with current provider (in case provider changed)
			if (AppStorage.TryLoad($"{AppStorageKeyPrefix}_{nameof(CacheWeatherProviderID)}", out int cacheWeatherProviderID))
			{
				CacheWeatherProviderID = cacheWeatherProviderID;
				isCachedDataMatchingWeatherProvider = CacheWeatherProviderID == Preferences.WeatherProviderId;
			}
			else
			{
				isCachedDataMatchingWeatherProvider = false;
			}

			// Loading the saved weather data, only if the location was loaded successfully and cache location matches
			if (isSavedLocationLoadSuccessful &&
				isCachedDataMatchingLocation &&
				isCachedDataMatchingWeatherProvider &&
				AppStorage.TryLoad($"{SettingsPrefix}_{nameof(CachedConditionCode)}", out int cachedConditionCode) &&
				AppStorage.TryLoad($"{SettingsPrefix}_{nameof(CachedIsDaytime)}", out bool cachedIsDaytime) &&
				AppStorage.TryLoad($"{SettingsPrefix}_{nameof(CachedTemperature)}", out double cachedTemperature) &&
				AppStorage.TryLoad($"{SettingsPrefix}_{nameof(CachedHourly)}", out List<HourlyForecast>? cachedHourly) && cachedHourly != null &&
				AppStorage.TryLoad($"{SettingsPrefix}_{nameof(CacheLastUpdateTime)}", out DateTime cacheLastUpdateTime))
			{
				CachedConditionCode = cachedConditionCode;
				CachedIsDaytime = cachedIsDaytime;
				CachedTemperature = cachedTemperature;
				CachedHourly = cachedHourly;
				CacheLastUpdateTime = cacheLastUpdateTime;
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

		public static void SetLocation(string newLocationName, string newLocationCountry, double newLocationLatitude, double newLocationLongitude)
		{
			LocationID = GetLocationId(newLocationLatitude, newLocationLongitude);
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

		public static async Task<(bool status, string message)> UpdateWeatherDataFromNetwork(bool skipCache, CancellationToken cancellationToken)
		{
			Debug.Assert(IsUpdatingFromNetwork is false, "UpdateWeatherDataFromNetwork called while another update is in progress.");

			if (IsCacheDataValid && IsCachedDataRecentEnough() && !skipCache)
			{
				return (true, "Using cached data."); // OK: no network update performed, because wasn't needed
			}

			IsUpdatingFromNetwork = true;

			try
			{
				cancellationToken.ThrowIfCancellationRequested();

				var provider = new Weather.OpenMeteo.OpenMeteoWeatherProvider();
				var result = await provider.GetWeatherAsync(LocationLatitude!.Value, LocationLongitude!.Value, cancellationToken);

				cancellationToken.ThrowIfCancellationRequested();

				if (result.weatherResult == null)
				{
					string errorMessage = $"Weather update failed: {result.errorMessage}";
					Debug.WriteLine(errorMessage);
					return (false, errorMessage); // ERROR: network error or something went wrong
				}

				// On success
				CacheLocationID = LocationID;
				CacheWeatherProviderID = provider.ProviderId;
				CachedConditionCode = result.weatherResult.Current.ConditionCode;
				CachedIsDaytime = result.weatherResult.Current.IsDaytime;
				CachedTemperature = result.weatherResult.Current.Temperature;
				CachedHourly = result.weatherResult.Hourly;
				CacheLastUpdateTime = DateTime.Now;

				IsUpdatingFromNetwork = false;
				IsCacheDataValid = true;
				SaveWeatherData();

				return (true, "OK"); // OK: data was updated from network
			}
			catch (OperationCanceledException)
			{
				string message = "Weather data update canceled.";
				Debug.WriteLine(message);
				return (true, message); // OK: was canceled, but no error
			}
			catch (Exception ex)
			{
				string message = $"Weather update failed: {ex}";
				Debug.WriteLine(message);
				return (false, message); // ERROR: failed with unexpected exception
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

			AppStorage.Save($"{AppStorageKeyPrefix}_{nameof(CacheLocationID)}", CacheLocationID);
			AppStorage.Save($"{AppStorageKeyPrefix}_{nameof(CacheWeatherProviderID)}", CacheWeatherProviderID);
			AppStorage.Save($"{AppStorageKeyPrefix}_{nameof(CachedConditionCode)}", CachedConditionCode);
			AppStorage.Save($"{AppStorageKeyPrefix}_{nameof(CachedIsDaytime)}", CachedIsDaytime);
			AppStorage.Save($"{AppStorageKeyPrefix}_{nameof(CachedTemperature)}", CachedTemperature);
			AppStorage.Save($"{AppStorageKeyPrefix}_{nameof(CachedHourly)}", CachedHourly);
			AppStorage.Save($"{AppStorageKeyPrefix}_{nameof(CacheLastUpdateTime)}", CacheLastUpdateTime);
			AppStorage.Flush();
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

		public static List<HourlyForecast> GetHourlyDataSinceNow()
		{
			Debug.Assert(IsCacheDataValid, "GetHourlyDataSinceNow called while weather data is not valid.");

			var now = DateTime.Now;

			return CachedHourly
				.Where(f => f.Time >= now)
				.ToList();
		}

		/// <summary>
		/// Location ID is a hash calculated from lat/long
		/// and used to determine if the correct location is data is cached.
		/// This way locations got from GeoData and from IPInfo can be kind-of compared.
		/// </summary>
		private static int GetLocationId(double latitude, double longitude)
		{
			// Round to avoid floating noise
			var lat = Math.Round(latitude, 4);
			var lon = Math.Round(longitude, 4);

			// Convert to long bits (stable numeric representation)
			long latBits = BitConverter.DoubleToInt64Bits(lat);
			long lonBits = BitConverter.DoubleToInt64Bits(lon);

			// Combine deterministically
			long hash = latBits ^ (lonBits * 31);

			// Compress to int
			return (int)(hash ^ (hash >> 32));
		}
	}
}
