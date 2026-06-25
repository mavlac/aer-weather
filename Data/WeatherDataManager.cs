using Aer.Weather;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aer.Data
{
	internal static class WeatherDataManager
	{
		private static WeatherData? weatherData;

		public static bool IsWeatherDataLoaded => weatherData is not null;
		public static bool IsUpdatingFromNetwork { get; set; }
		public static WeatherData WeatherData
		{
			get
			{
				if (weatherData == null)
				{
					throw new InvalidOperationException("Weather data is not loaded. Call Load() before accessing WeatherData.");
				}
				return weatherData;
			}
		}

		public static async Task<(bool status, string errorMessage)> Load(CancellationToken cancellationToken)
		{
			if (LocationManager.CurrentLocation == null)
			{
				throw new InvalidOperationException("LocationManager CurrentLocation is not set, before loading data from cache.");
			}
			if (Preferences.WeatherProviderId == null)
			{
				throw new InvalidOperationException("Preferences WeatherProviderId is not set, before loading data from cache.");
			}

			var response =
				WeatherDataCache.GetWeatherData(
					LocationManager.CurrentLocation.ID,
					Preferences.WeatherProviderId.Value,
					out weatherData);
			switch (response)
			{
				// Cache loaded
				case WeatherDataCache.GetWeatherDataResponse.IsLoaded:
					Debug.WriteLine("Using cached data.");
					return (true, string.Empty);
				
				// Cache loaded, but expired
				case WeatherDataCache.GetWeatherDataResponse.IsLoadedButExpired:
					Debug.WriteLine("Using expired cached data. Updating from network...");
					return await UpdateWeatherDataFromNetwork(cancellationToken);
				
				// No valid cache data or load fail
				default:
					Debug.WriteLine("No cached data found. Updating from network...");
					return await UpdateWeatherDataFromNetwork(cancellationToken);
			}
		}

		private static async Task<(bool status, string errorMessage)> UpdateWeatherDataFromNetwork(CancellationToken cancellationToken)
		{
			Debug.Assert(IsUpdatingFromNetwork is false, "UpdateWeatherDataFromNetwork called while another update is in progress.");

			IsUpdatingFromNetwork = true;

			// Simulate a long-running operation
			//await Task.Delay(3000, cancellationToken);

			try
			{
				cancellationToken.ThrowIfCancellationRequested();

				var provider = WeatherProvider.Get(Preferences.WeatherProviderId!.Value);
				var (weatherResult, errorMessage) = await provider.GetWeatherAsync(LocationManager.CurrentLocation!.Latitude, LocationManager.CurrentLocation!.Longitude, cancellationToken);

				cancellationToken.ThrowIfCancellationRequested();

				if (weatherResult == null)
				{
					Debug.WriteLine($"Weather update failed: {errorMessage}");
					return (false, errorMessage); // ERROR: network error, parsing error or something went wrong
				}

				// Success
				weatherData = new WeatherData
				{
					LocationID = LocationManager.CurrentLocation.ID,
					WeatherProviderID = provider.ProviderId,
					Created = DateTimeOffset.UtcNow,
					ValidUntil = weatherResult.ValidUntil,
					CurrentWeather = weatherResult.Current,
					HourlyForecast = weatherResult.Hourly
				};

				WeatherDataCache.SaveWeatherData(weatherData);
				Debug.WriteLine($"Weather update completed successfully.");
				return (true, string.Empty); // OK: data was updated from network
			}
			catch (OperationCanceledException ex)
			{
				Debug.WriteLine($"Weather update canceled: {ex}");
				return (true, string.Empty); // OK: was canceled, but no error
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

		public static List<HourlyForecast> GetHourlyDataSinceNow()
		{
			if (weatherData == null)
			{
				throw new InvalidOperationException("GetHourlyDataSinceNow called while no data is loaded.");
			}
			
			var now = DateTime.Now;
			
			return weatherData.HourlyForecast.Where(f => f.Time >= now).ToList();
		}
	}
}
