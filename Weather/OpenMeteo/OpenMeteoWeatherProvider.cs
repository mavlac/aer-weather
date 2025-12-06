using Aer.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Aer.Weather.OpenMeteo
{
	public class OpenMeteoWeatherProvider : WeatherProvider
	{
		public const int ProviderStaticId = 0;
		public override int ProviderId => ProviderStaticId;
		public override string ProviderName => "Open-Meteo";
		public override string ProviderDescription => "Global coverage";

		public override async Task<(WeatherResult? weatherResult, string errorMessage)> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken)
		{
			var (openMeteoResponse, errorMessage) = await OpenMeteoNetworkQuery(latitude, longitude, cancellationToken);
			if (openMeteoResponse == null ||
				openMeteoResponse.Current == null ||
				openMeteoResponse.Hourly == null)
			{
				Debug.WriteLine("Response is null or something went wrong when parsing it.");
				return (null, errorMessage);
			}

			Debug.WriteLine($"Got response '{openMeteoResponse.Current.Time}' UTC → '{DateTimeUtils.ConvertUtcIsoToLocal(openMeteoResponse.Current.Time)}' {TimeZoneInfo.Local.DisplayName}, {TimeZoneInfo.Local.DaylightName}");

			// Map current
			var current = new CurrentWeatherData
			{
				IsDaytime = openMeteoResponse.Current.IsDay == 1,
				Temperature = openMeteoResponse.Current.Temperature,
				ConditionCode = openMeteoResponse.Current.WeatherCode,
			};

			// Map hourly
			var hourly = new List<HourlyForecast>();
			for (int i = 0; i < openMeteoResponse.Hourly.Time.Count; i++)
			{
				hourly.Add(new HourlyForecast
				{
					Time = DateTimeUtils.ConvertUtcIsoToLocal(openMeteoResponse.Hourly.Time[i]), // Convert Open-Meteo's UTC time to local OS time
					IsDaytime = openMeteoResponse.Hourly.IsDay[i] == 1,
					Temperature = openMeteoResponse.Hourly.Temperature[i],
					ConditionCode = openMeteoResponse.Hourly.WeatherCode[i],
					Rain = openMeteoResponse.Hourly.Rain[i],
					Snowfall = openMeteoResponse.Hourly.Snowfall[i]
				});
			}

			return (new WeatherResult { Current = current, Hourly = hourly }, string.Empty);
		}

		private async Task<(OpenMeteoResponse? openMeteoResponse, string errorMessage)> OpenMeteoNetworkQuery(double latitude, double longitude, CancellationToken cancellationToken)
		{
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				
				// Fetch weather data from Open-Meteo API
				const int days = 7; // Number of forecast days to retrieve
				string url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}&current_weather=true&hourly=temperature_2m,weather_code,rain,snowfall,is_day&timezone=GMT&temperature_unit=celsius&forecast_days={days}";
				string json = await _httpClient.GetStringAsync(url, cancellationToken);
				
				return (JsonSerializer.Deserialize<OpenMeteoResponse>(json), string.Empty);
			}
			catch (OperationCanceledException)
			{
				return (null, "Operation cancelled");
			}
			catch (HttpRequestException ex)
			{
				return (null, $"Network error: {ex.Message}");
			}
			catch (Exception ex)
			{
				return (null, $"Unexpected Open-Meteo API error: {ex.Message}");
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
