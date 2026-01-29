using Aer.Utils;
using Aer.Weather;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aer.Data
{
	// TODO: Update the text, AppStorage will be replaced by DB
	/// <summary>
	/// Cached weather data, saved using AppStorage
	/// Able to update itself from network using the IWeatherProvider
	/// </summary>
	internal class WeatherData
	{
		private const string AppStorageKeyPrefix = nameof(WeatherData);

		private static bool IsUpdatingFromNetwork { get; set; }

		// TODO: Wrap all this in some sort of "Current" record
		public static int? CacheLocationID { get; private set; }
		public static int? CacheWeatherProviderID { get; private set; }
		public static int? CachedWeatherCode { get; private set; }
		public static bool? CachedIsDaytime { get; private set; }
		public static double? CachedTemperature { get; private set; } // Stored in Celsius. Converted if shown as Fahrenheit
		public static List<HourlyForecast> CachedHourly { get; private set; } = new();
		public static DateTimeOffset? CacheValidUntil { get; private set; }
		public static DateTimeOffset? CacheLastUpdateTime { get; private set; }
		
		public static bool IsCachedDataLoaded => CacheValidUntil is not null;
		public static bool IsCachedDataRecentEnough => IsCachedDataLoaded && DateTimeOffset.UtcNow < CacheValidUntil;

		public static string ReadableTemperature => CachedTemperature is null ? "—" : TemperatureUtils.GetReadableTemperature(CachedTemperature.Value, " ", 0);
		public static string ConditionDescription => CachedWeatherCode is null ? "—" : WeatherDescriptions.GetDescription(CachedWeatherCode.Value, CachedIsDaytime!.Value);
		public static string ConditionWeatherIconsGlyph => CachedWeatherCode is null || CachedIsDaytime is null ? WeatherIconsUtils.Unknown : WeatherIconsUtils.GetWeatherIcon(CachedWeatherCode!.Value, CachedIsDaytime!.Value);

		public static void LoadOrSetDefaults()
		{
			bool isCachedDataMatchingLocation;
			bool isCachedDataMatchingWeatherProvider;

			// Getting the cached location to compare with current location
			if (AppStorage.TryGetValue($"{AppStorageKeyPrefix}_{nameof(CacheLocationID)}", out int cacheLocationID))
			{
				CacheLocationID = cacheLocationID;
				isCachedDataMatchingLocation = (CacheLocationID == Location.ID);
			}
			else
			{
				isCachedDataMatchingLocation = false;
			}

			// Getting the cached weather provider ID to compare with current provider
			if (AppStorage.TryGetValue($"{AppStorageKeyPrefix}_{nameof(CacheWeatherProviderID)}", out int cacheWeatherProviderID))
			{
				CacheWeatherProviderID = cacheWeatherProviderID;
				isCachedDataMatchingWeatherProvider = (CacheWeatherProviderID == Preferences.WeatherProviderId);
			}
			else
			{
				isCachedDataMatchingWeatherProvider = false;
			}

			// Loading the saved weather data, only if the location was loaded successfully and cache location matches
			if (isCachedDataMatchingLocation &&
				isCachedDataMatchingWeatherProvider &&
				AppStorage.TryGetValue($"{AppStorageKeyPrefix}_{nameof(CachedWeatherCode)}", out int cachedWeatherCode) &&
				AppStorage.TryGetValue($"{AppStorageKeyPrefix}_{nameof(CachedIsDaytime)}", out bool cachedIsDaytime) &&
				AppStorage.TryGetValue($"{AppStorageKeyPrefix}_{nameof(CachedTemperature)}", out double cachedTemperature) &&
				AppStorage.TryGetValue($"{AppStorageKeyPrefix}_{nameof(CachedHourly)}", out List<HourlyForecast>? cachedHourly) && cachedHourly != null &&
				AppStorage.TryGetValue($"{AppStorageKeyPrefix}_{nameof(CacheValidUntil)}", out DateTime cacheValidUntil) &&
				AppStorage.TryGetValue($"{AppStorageKeyPrefix}_{nameof(CacheLastUpdateTime)}", out DateTime cacheLastUpdateTime))
			{
				CachedWeatherCode = cachedWeatherCode;
				CachedIsDaytime = cachedIsDaytime;
				CachedTemperature = cachedTemperature;
				CachedHourly = cachedHourly;
				CacheValidUntil = cacheValidUntil;
				CacheLastUpdateTime = cacheLastUpdateTime;
			}
			else
			{
				// Cached data load failed, or cached from another location/provider
				CacheValidUntil = null;
				CacheLastUpdateTime = null;
			}
			
			Debug.WriteLineIf(IsCachedDataLoaded, $"WeatherData loaded and valid. All OK.");
			Debug.WriteLineIf(!IsCachedDataLoaded, $"Loading from the cache failed.");
			Debug.WriteLineIf(!isCachedDataMatchingLocation, $"WeatherData location not matching selected location. '{CacheLocationID}' vs '{Location.ID}'");
		}

		public static async Task<(bool status, string message)> UpdateWeatherDataFromNetwork(CancellationToken cancellationToken)
		{
			Debug.Assert(IsUpdatingFromNetwork is false, "UpdateWeatherDataFromNetwork called while another update is in progress.");

			if (IsCachedDataLoaded && IsCachedDataRecentEnough)
			{
				return (true, "Using cached data."); // OK: no network update performed, because wasn't needed
			}

			IsUpdatingFromNetwork = true;

			// Simulate a long-running operation
			//await Task.Delay(1000);

			try
			{
				cancellationToken.ThrowIfCancellationRequested();

				var provider = WeatherProvider.Get(Preferences.WeatherProviderId);
				var (weatherResult, errorMessage) = await provider.GetWeatherAsync(Location.Latitude!.Value, Location.Longitude!.Value, cancellationToken);

				cancellationToken.ThrowIfCancellationRequested();

				if (weatherResult == null)
				{
					Debug.WriteLine($"Weather update failed: {errorMessage}");
					return (false, errorMessage); // ERROR: network error, parsing error or something went wrong
				}

				// On success
				CacheLocationID = Location.ID;
				CacheWeatherProviderID = provider.ProviderId;
				CachedWeatherCode = weatherResult.Current.WeatherCode;
				CachedIsDaytime = weatherResult.Current.IsDaytime;
				CachedTemperature = weatherResult.Current.Temperature;
				CachedHourly = weatherResult.Hourly;
				CacheValidUntil = weatherResult.ValidUntil;
				CacheLastUpdateTime = DateTimeOffset.UtcNow;

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
			Debug.Assert(IsCachedDataLoaded, "SaveWeatherData called while weather data is not loaded.");

			AppStorage.SaveValue($"{AppStorageKeyPrefix}_{nameof(CacheLocationID)}", CacheLocationID);
			AppStorage.SaveValue($"{AppStorageKeyPrefix}_{nameof(CacheWeatherProviderID)}", CacheWeatherProviderID);
			AppStorage.SaveValue($"{AppStorageKeyPrefix}_{nameof(CachedWeatherCode)}", CachedWeatherCode);
			AppStorage.SaveValue($"{AppStorageKeyPrefix}_{nameof(CachedIsDaytime)}", CachedIsDaytime);
			AppStorage.SaveValue($"{AppStorageKeyPrefix}_{nameof(CachedTemperature)}", CachedTemperature);
			AppStorage.SaveValue($"{AppStorageKeyPrefix}_{nameof(CachedHourly)}", CachedHourly);
			AppStorage.SaveValue($"{AppStorageKeyPrefix}_{nameof(CacheValidUntil)}", CacheValidUntil);
			AppStorage.SaveValue($"{AppStorageKeyPrefix}_{nameof(CacheLastUpdateTime)}", CacheLastUpdateTime);
			AppStorage.Flush();
		}

		public static List<HourlyForecast> GetHourlyDataSinceNow()
		{
			Debug.Assert(IsCachedDataLoaded, "GetHourlyDataSinceNow called while no weather data is loaded.");

			var now = DateTime.Now;

			return CachedHourly.Where(f => f.Time >= now).ToList();
		}
	}
}
