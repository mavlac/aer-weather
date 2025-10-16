using Aer.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Aer.Weather.OpenMeteo
{
	public class OpenMeteoWeatherProvider : IWeatherProvider
	{
		private readonly HttpClient _client;

		public OpenMeteoWeatherProvider(HttpClient? client = null)
		{
			_client = client ?? new HttpClient();
		}

		public async Task<WeatherResult?> GetWeatherAsync(double latitude, double longitude)
		{
			var response = await OpenMeteoNetworkQuery(latitude, longitude);
			if (response == null || response.Current == null || response.Hourly == null)
			{
				Debug.WriteLine("Response is null or something went wrong when parsing it.");
				return null;
			}

			Debug.WriteLine($"Got response '{response.Current.Time}' UTC → '{DateTimeUtils.ConvertUtcIsoToLocal(response.Current.Time)}' {TimeZoneInfo.Local.DisplayName}, {TimeZoneInfo.Local.DaylightName}");

			// Map current
			var current = new CurrentWeatherData
			{
				IsDaytime = response.Current.IsDay == 1,
				Temperature = response.Current.Temperature,
				ConditionCode = response.Current.WeatherCode,
				ObservationTime = response.Current.Time
			};

			// Map hourly
			var hourly = new List<HourlyForecast>();
			for (int i = 0; i < response.Hourly.Time.Count; i++)
			{
				hourly.Add(new HourlyForecast
				{
					Time = DateTimeUtils.ConvertUtcIsoToLocal(response.Hourly.Time[i]), // Convert Open-Meteo's UTC time to local OS time
					IsDaytime = response.Hourly.IsDay[i] == 1,
					Temperature = response.Hourly.Temperature[i],
					ConditionCode = response.Hourly.WeatherCode[i],
					Rain = response.Hourly.Rain[i],
					Snowfall = response.Hourly.Snowfall[i]
				});
			}

			return new WeatherResult { Current = current, Hourly = hourly };
		}

		private async Task<OpenMeteoResponse?> OpenMeteoNetworkQuery(double latitude, double longitude)
		{
			try
			{
				// Fetch weather data from Open-Meteo API
				string url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}&current_weather=true&hourly=temperature_2m,weather_code,rain,snowfall,is_day&timezone=auto&temperature_unit=celsius&forecast_days=1";
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
			[JsonPropertyName("current_weather")] public OpenMeteoCurrent? Current { get; set; }
			[JsonPropertyName("hourly")] public OpenMeteoHourly? Hourly { get; set; }
		}

		public class OpenMeteoCurrent
		{
			/// <summary>
			/// Is GMT+0 / UTC when no timezone specified in query
			/// </summary>
			[JsonPropertyName("time")] public string Time { get; set; } = string.Empty;
			/// <summary>
			/// Is Celsius when no temperature_unit specified in query
			/// </summary>
			[JsonPropertyName("temperature")] public double Temperature { get; set; }
			[JsonPropertyName("is_day")] public int IsDay { get; set; }
			[JsonPropertyName("weathercode")] public int WeatherCode { get; set; }
		}

		public class OpenMeteoHourly
		{
			/// <summary>
			/// Is GMT+0 / UTC when no timezone specified in query
			/// </summary>
			[JsonPropertyName("time")] public List<string> Time { get; set; } = new(); // Is GMT+0 / UTC when no timezone specified in query
			/// <summary>
			/// Is Celsius when no temperature_unit specified in query
			/// </summary>
			[JsonPropertyName("temperature_2m")] public List<double> Temperature { get; set; } = new();
			[JsonPropertyName("weather_code")] public List<int> WeatherCode { get; set; } = new();
			[JsonPropertyName("rain")] public List<double> Rain { get; set; } = new();
			[JsonPropertyName("snowfall")] public List<double> Snowfall { get; set; } = new();
			[JsonPropertyName("is_day")] public List<int> IsDay { get; set; } = new();
		}
		#endregion
	}
}
