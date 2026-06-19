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

		public static async Task<(bool status, string message)> Load(CancellationToken cancellationToken)
		{
			if (!Location.ID.HasValue || !Preferences.WeatherProviderId.HasValue)
			{
				throw new InvalidOperationException("Location ID or Weather Provider ID is not set or set default, before loading data from cache.");
			}

			bool success =
				WeatherDataCache.GetWeatherData(
					Location.ID.Value,
					Preferences.WeatherProviderId.Value,
					out weatherData);
			if (success)
			{
				return (true, "Using cached data."); // OK: using valid data from cache
			}
			else
			{
				// No valid cache data or load fail
				return await UpdateWeatherDataFromNetwork(cancellationToken);
			}
		}

		private static async Task<(bool status, string message)> UpdateWeatherDataFromNetwork(CancellationToken cancellationToken)
		{
			Debug.Assert(IsUpdatingFromNetwork is false, "UpdateWeatherDataFromNetwork called while another update is in progress.");

			IsUpdatingFromNetwork = true;

			// Simulate a long-running operation
			//await Task.Delay(1000);

			try
			{
				cancellationToken.ThrowIfCancellationRequested();

				var provider = WeatherProvider.Get(Preferences.WeatherProviderId!.Value);
				var (weatherResult, errorMessage) = await provider.GetWeatherAsync(Location.Latitude!.Value, Location.Longitude!.Value, cancellationToken);

				cancellationToken.ThrowIfCancellationRequested();

				if (weatherResult == null)
				{
					Debug.WriteLine($"Weather update failed: {errorMessage}");
					return (false, errorMessage); // ERROR: network error, parsing error or something went wrong
				}

				// Success
				weatherData = new WeatherData
				{
					LocationID = Location.ID!.Value,
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
