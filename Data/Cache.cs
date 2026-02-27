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
	internal class Cache
	{
		private const string AppStorageKeyPrefix = nameof(Cache);

		private static bool IsUpdatingFromNetwork { get; set; }

		// Cache
		public static int? CacheLocationID { get; private set; }
		public static int? CacheWeatherProviderID { get; private set; }
		public static CurrentWeather? CurrentWeather { get; private set; }
		public static List<HourlyForecast> HourlyForecast { get; private set; } = new();
		public static DateTimeOffset? CacheValidUntil { get; private set; }
		public static DateTimeOffset? CacheLastUpdateTime { get; private set; }
		
		public static bool IsCachedDataLoaded => CacheValidUntil is not null;
		public static bool IsCachedDataRecentEnough => IsCachedDataLoaded && DateTimeOffset.UtcNow < CacheValidUntil;

		public static string ReadableTemperature => CurrentWeather is null ? "—" : TemperatureUtils.GetReadableTemperature(CurrentWeather.Temperature, " ", 0);
		public static string ReadableApparentTemperature => CurrentWeather is null ? "—" : TemperatureUtils.GetReadableTemperature(CurrentWeather.ApparentTemperature, " ", 0);
		public static string ConditionDescription => CurrentWeather is null ? "—" : WeatherDescriptions.GetDescription(CurrentWeather.WeatherCode, CurrentWeather.IsDaytime);
		public static string ConditionWeatherIconsGlyph => CurrentWeather is null ? WeatherIconsUtils.Unknown : WeatherIconsUtils.GetWeatherIcon(CurrentWeather.WeatherCode, CurrentWeather.IsDaytime);

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
				AppStorage.TryGetValue($"{AppStorageKeyPrefix}_{nameof(CurrentWeather)}", out CurrentWeather? cachedCurrent) && cachedCurrent != null &&
				AppStorage.TryGetValue($"{AppStorageKeyPrefix}_{nameof(HourlyForecast)}", out List<HourlyForecast>? cachedHourly) && cachedHourly != null &&
				AppStorage.TryGetValue($"{AppStorageKeyPrefix}_{nameof(CacheValidUntil)}", out DateTime cacheValidUntil) &&
				AppStorage.TryGetValue($"{AppStorageKeyPrefix}_{nameof(CacheLastUpdateTime)}", out DateTime cacheLastUpdateTime))
			{
				CurrentWeather = cachedCurrent;
				HourlyForecast = cachedHourly;
				CacheValidUntil = cacheValidUntil;
				CacheLastUpdateTime = cacheLastUpdateTime;
			}
			else
			{
				// Cached data load failed, or cached from another location/provider
				CacheValidUntil = null;
				CacheLastUpdateTime = null;
			}
			
			Debug.WriteLineIf(IsCachedDataLoaded, $"Cache loaded and valid. All OK.");
			Debug.WriteLineIf(!IsCachedDataLoaded, $"Loading from the cache failed.");
			Debug.WriteLineIf(!isCachedDataMatchingLocation, $"Cache location not matching selected location. '{CacheLocationID}' vs '{Location.ID}'");
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
				CurrentWeather = weatherResult.Current;
				HourlyForecast = weatherResult.Hourly;
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
			Debug.Assert(CurrentWeather is not null, "SaveWeatherData called while CurrentWeather is null.");

			AppStorage.SaveValue($"{AppStorageKeyPrefix}_{nameof(CacheLocationID)}", CacheLocationID);
			AppStorage.SaveValue($"{AppStorageKeyPrefix}_{nameof(CacheWeatherProviderID)}", CacheWeatherProviderID);
			AppStorage.SaveValue($"{AppStorageKeyPrefix}_{nameof(CurrentWeather)}", CurrentWeather);
			AppStorage.SaveValue($"{AppStorageKeyPrefix}_{nameof(HourlyForecast)}", HourlyForecast);
			AppStorage.SaveValue($"{AppStorageKeyPrefix}_{nameof(CacheValidUntil)}", CacheValidUntil);
			AppStorage.SaveValue($"{AppStorageKeyPrefix}_{nameof(CacheLastUpdateTime)}", CacheLastUpdateTime);
			AppStorage.Flush();
		}

		public static List<HourlyForecast> GetHourlyDataSinceNow()
		{
			Debug.Assert(IsCachedDataLoaded, "GetHourlyDataSinceNow called while no weather data is loaded.");

			var now = DateTime.Now;

			return HourlyForecast.Where(f => f.Time >= now).ToList();
		}
	}
}
