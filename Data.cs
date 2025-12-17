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
		public static int? CachedWeatherCode { get; private set; }
		public static bool? CachedIsDaytime { get; private set; }
		public static double? CachedTemperature { get; private set; } // Stored in Celsius. Converted if shown as Fahrenheit
		public static List<HourlyForecast> CachedHourly { get; private set; } = new();
		public static DateTimeOffset? CacheValidUntil { get; private set; }
		public static DateTimeOffset? CacheLastUpdateTime { get; private set; }
		public static bool IsCacheDataValid { get; private set; }
		public static bool IsCachedDataRecentEnough => CacheValidUntil is not null && DateTimeOffset.UtcNow < CacheValidUntil;

		public static string? LocationCoordinates => LocationLatitude is null || LocationLongitude is null ? null : $"{LocationLatitude.Value.ToString("F4", CultureInfo.InvariantCulture)}, {LocationLongitude.Value.ToString("F4", CultureInfo.InvariantCulture)}";
		public static string ReadableTemperature => CachedTemperature is null ? "—" : TemperatureUtils.GetReadableTemperature(CachedTemperature.Value, " ");
		public static string ConditionDescription => CachedWeatherCode is null ? "—" : WeatherDescriptions.GetDescription(CachedWeatherCode.Value, CachedIsDaytime!.Value);
		public static string ConditionWeatherIconsGlyph => CachedWeatherCode is null || CachedIsDaytime is null ? WeatherIconsUtils.Unknown : WeatherIconsUtils.GetWeatherIcon(CachedWeatherCode!.Value, CachedIsDaytime!.Value);

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
				AppStorage.TryLoad($"{AppStorageKeyPrefix}_{nameof(CachedWeatherCode)}", out int cachedWeatherCode) &&
				AppStorage.TryLoad($"{AppStorageKeyPrefix}_{nameof(CachedIsDaytime)}", out bool cachedIsDaytime) &&
				AppStorage.TryLoad($"{AppStorageKeyPrefix}_{nameof(CachedTemperature)}", out double cachedTemperature) &&
				AppStorage.TryLoad($"{AppStorageKeyPrefix}_{nameof(CachedHourly)}", out List<HourlyForecast>? cachedHourly) && cachedHourly != null &&
				AppStorage.TryLoad($"{AppStorageKeyPrefix}_{nameof(CacheValidUntil)}", out DateTime cacheValidUntil) &&
				AppStorage.TryLoad($"{AppStorageKeyPrefix}_{nameof(CacheLastUpdateTime)}", out DateTime cacheLastUpdateTime))
			{
				CachedWeatherCode = cachedWeatherCode;
				CachedIsDaytime = cachedIsDaytime;
				CachedTemperature = cachedTemperature;
				CachedHourly = cachedHourly;
				CacheValidUntil = cacheValidUntil;
				CacheLastUpdateTime = cacheLastUpdateTime;
				IsCacheDataValid = true;
			}
			else
			{
				// Location set to defaults
				// or cached data from another location
				// or no success loading the cache
				CacheValidUntil = null;
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

		public static async Task<(bool status, string message)> UpdateWeatherDataFromNetwork(CancellationToken cancellationToken)
		{
			Debug.Assert(IsUpdatingFromNetwork is false, "UpdateWeatherDataFromNetwork called while another update is in progress.");

			if (IsCacheDataValid && IsCachedDataRecentEnough)
			{
				return (true, "Using cached data."); // OK: no network update performed, because wasn't needed
			}

			IsUpdatingFromNetwork = true;

			try
			{
				cancellationToken.ThrowIfCancellationRequested();

				var provider = WeatherProvider.Get(Preferences.WeatherProviderId);
				var (weatherResult, errorMessage) = await provider.GetWeatherAsync(LocationLatitude!.Value, LocationLongitude!.Value, cancellationToken);

				cancellationToken.ThrowIfCancellationRequested();

				if (weatherResult == null)
				{
					Debug.WriteLine($"Weather update failed: {errorMessage}");
					return (false, errorMessage); // ERROR: network error, parsing error or something went wrong
				}

				// On success
				CacheLocationID = LocationID;
				CacheWeatherProviderID = provider.ProviderId;
				CachedWeatherCode = weatherResult.Current.WeatherCode;
				CachedIsDaytime = weatherResult.Current.IsDaytime;
				CachedTemperature = weatherResult.Current.Temperature;
				CachedHourly = weatherResult.Hourly;
				CacheValidUntil = weatherResult.ValidUntil;
				CacheLastUpdateTime = DateTimeOffset.UtcNow;

				IsCacheDataValid = true;
				IsUpdatingFromNetwork = false; // Needs to be set here and not in finally, because SaveWeatherData asserts this
				SaveWeatherData();

				return (true, string.Empty); // OK: data was updated from network
			}
			catch (OperationCanceledException ex)
			{
				Debug.WriteLine($"Weather update canceled: {ex}");
				return (true, ex.Message); // OK: was canceled, but no error
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Weather update failed: {ex}");
				return (false, ex.Message); // ERROR: failed with unexpected exception
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
			AppStorage.Save($"{AppStorageKeyPrefix}_{nameof(CachedWeatherCode)}", CachedWeatherCode);
			AppStorage.Save($"{AppStorageKeyPrefix}_{nameof(CachedIsDaytime)}", CachedIsDaytime);
			AppStorage.Save($"{AppStorageKeyPrefix}_{nameof(CachedTemperature)}", CachedTemperature);
			AppStorage.Save($"{AppStorageKeyPrefix}_{nameof(CachedHourly)}", CachedHourly);
			AppStorage.Save($"{AppStorageKeyPrefix}_{nameof(CacheValidUntil)}", CacheValidUntil);
			AppStorage.Save($"{AppStorageKeyPrefix}_{nameof(CacheLastUpdateTime)}", CacheLastUpdateTime);
			AppStorage.Flush();
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
			var lat = Math.Round(latitude, 3);
			var lon = Math.Round(longitude, 3);

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
