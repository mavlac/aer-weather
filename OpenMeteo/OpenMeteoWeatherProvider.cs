using Aer.Weather;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Aer.OpenMeteo
{
	public class OpenMeteoWeatherProvider : IWeatherProvider
	{
		private const string CacheJsonKey = "OpenMeteo_CachedJson";
		private const string CacheTimeKey = "OpenMeteo_CachedTime";

		private readonly HttpClient _client;

		public OpenMeteoWeatherProvider(HttpClient? client = null)
		{
			_client = client ?? new HttpClient();
		}

		public async Task<WeatherResult?> GetWeatherAsync(double latitude, double longitude)
		{
			var response = await FetchAsync(latitude, longitude);
			if (response == null || response.Current == null || response.Hourly == null)
				return null;

			// Map current
			var current = new WeatherData
			{
				Temperature = response.Current.Temperature_2m,
				ConditionCode = response.Current.Weather_Code,
				ObservationTime = response.Current.Time
			};

			// Map hourly
			var hourly = new List<HourlyForecast>();
			for (int i = 0; i < response.Hourly.Time.Count; i++)
			{
				hourly.Add(new HourlyForecast
				{
					Time = response.Hourly.Time[i],
					Temperature = response.Hourly.Temperature_2m[i],
					ConditionCode = response.Hourly.Weather_Code[i],
					Rain = response.Hourly.Rain[i],
					Snowfall = response.Hourly.Snowfall[i]
				});
			}

			return new WeatherResult { Current = current, HourlyForecast = hourly };
		}

		private async Task<OpenMeteoResponse?> FetchAsync(double latitude, double longitude)
		{
			try
			{
				// Fetch weather data from Open-Meteo API
				string url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}&current_weather=true&hourly=temperature_2m,weather_code,rain,snowfall&timezone=auto&temperature_unit=celsius";
				string json = await _client.GetStringAsync(url);
				
				return JsonSerializer.Deserialize<OpenMeteoResponse>(json);
			}
			catch (HttpRequestException ex)
			{
				Debug.WriteLine($"Network error: {ex.Message}");
				return null;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Unexpected error: {ex}");
				return null;
			}
		}

		#region OpenMeteo JSON Models

		public class OpenMeteoResponse
		{
			public OpenMeteoCurrent? Current { get; set; }
			public OpenMeteoHourly? Hourly { get; set; }
		}

		public class OpenMeteoCurrent
		{
			public string Time { get; set; } = string.Empty;
			public double Temperature_2m { get; set; }
			public int Weather_Code { get; set; }
		}

		public class OpenMeteoHourly
		{
			public List<string> Time { get; set; } = new();
			public List<double> Temperature_2m { get; set; } = new();
			public List<int> Weather_Code { get; set; } = new();
			public List<double> Rain { get; set; } = new();
			public List<double> Snowfall { get; set; } = new();
		}
		#endregion
	}
}
