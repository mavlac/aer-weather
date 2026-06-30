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
	/// <summary>
	/// Weather provider for Open-Meteo API (https://open-meteo.com/en/docs).
	/// </summary>
	public class OpenMeteoWeatherProvider : WeatherProvider
	{
		public const int ProviderStaticId = 0;

		public override int ProviderId => ProviderStaticId;
		public override string ProviderName => "Open-Meteo";
		public override string ProviderURL => "api.open-meteo.com";
		public override string ProviderDescription => "Fast with global coverage";

		public override async Task<(WeatherResult? weatherResult, string errorMessage)> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken)
		{
			var (openMeteoResponse, errorMessage) = await OpenMeteoNetworkQuery(latitude, longitude, cancellationToken);
			if (openMeteoResponse == null ||
				openMeteoResponse.Current == null ||
				openMeteoResponse.Hourly == null)
			{
				return (null, errorMessage ?? "Response parsed incomplete");
			}

			Debug.WriteLine($"Got response '{openMeteoResponse.Current.Time}' UTC → '{DateTimeUtils.ConvertUtcIsoToLocal(openMeteoResponse.Current.Time)}' {TimeZoneInfo.Local.DisplayName}, {TimeZoneInfo.Local.DaylightName}");

			// Map current
			var current = new CurrentWeather
			{
				IsDaytime = openMeteoResponse.Current.IsDay == 1.0,
				Temperature = openMeteoResponse.Current.Temperature,
				ApparentTemperature = openMeteoResponse.Current.ApparentTemperature,
				WeatherCode = openMeteoResponse.Current.WeatherCode,
			};

			// Map hourly
			var hourly = new List<HourlyForecast>();
			for (int i = 0; i < openMeteoResponse.Hourly.Time.Count; i++)
			{
				hourly.Add(new HourlyForecast
				{
					Time = DateTimeUtils.ConvertUtcIsoToLocal(openMeteoResponse.Hourly.Time[i]), // Convert Open-Meteo's UTC time to local OS time
					IsDaytime = openMeteoResponse.Hourly.IsDay[i] == 1.0,
					Temperature = openMeteoResponse.Hourly.Temperature[i],
					ApparentTemperature = openMeteoResponse.Hourly.ApparentTemperature[i],
					WeatherCode = openMeteoResponse.Hourly.WeatherCode[i],
					Rain = openMeteoResponse.Hourly.Rain[i],
					Snowfall = openMeteoResponse.Hourly.Snowfall[i]
				});
			}

			// Validity (calculated), UTC0
			var validUntil = DateTimeOffset.UtcNow.AddMinutes(DefaultCacheValidityMinutes);

			return (new WeatherResult(current, hourly, validUntil), string.Empty);
		}

		/// <summary>
		/// The network call
		/// </summary>
		private async Task<(OpenMeteoResponse? openMeteoResponse, string errorMessage)> OpenMeteoNetworkQuery(double latitude, double longitude, CancellationToken cancellationToken)
		{
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				
				// Fetch weather data from Open-Meteo API
				string url =
					$"https://api.open-meteo.com/v1/forecast" +
					$"?latitude={latitude.ToString("F4", CultureInfo.InvariantCulture)}" +
					$"&longitude={longitude.ToString("F4", CultureInfo.InvariantCulture)}" +
					$"&current=temperature_2m,apparent_temperature,is_day,weather_code" +
					$"&hourly=temperature_2m,apparent_temperature,is_day,weather_code,rain,snowfall" +
					$"&timezone=GMT" +
					$"&temperature_unit=celsius" +
					$"&forecast_days=7";

				var response = await _httpClient.GetAsync(url, cancellationToken);
				string json = await response.Content.ReadAsStringAsync(cancellationToken);
				var openMeteoResponse = JsonSerializer.Deserialize<OpenMeteoResponse>(json, _jsonOptions);
				return (openMeteoResponse, string.Empty);
			}
			catch (OperationCanceledException ex) when (ex is TaskCanceledException)
			{
				// Could be timeout or manual cancellation
				if (!cancellationToken.IsCancellationRequested)
				{
					return (null, "Operation timed out");
				}
				else
				{
					return (null, "Operation cancelled by user");
				}
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
			[JsonPropertyName("current")] public OpenMeteoCurrent? Current { get; set; }
			[JsonPropertyName("hourly")] public OpenMeteoHourly? Hourly { get; set; }
		}

		public class OpenMeteoCurrent
		{
			/// <summary>
			/// Is GMT+0, specified in query
			/// </summary>
			[JsonPropertyName("time")] public string Time { get; set; } = string.Empty;
			/// <summary>
			/// Is Celsius, specified in query
			/// </summary>
			[JsonPropertyName("temperature_2m")] public double Temperature { get; set; }
			/// <summary>
			/// Is Celsius, specified in query
			/// </summary>
			[JsonPropertyName("apparent_temperature")] public double ApparentTemperature { get; set; }
			[JsonPropertyName("is_day")] public double IsDay { get; set; }
			[JsonPropertyName("weather_code")] public int WeatherCode { get; set; }
		}

		public class OpenMeteoHourly
		{
			/// <summary>
			/// Is GMT+0, specified in query
			/// </summary>
			[JsonPropertyName("time")] public List<string> Time { get; set; } = new();
			/// <summary>
			/// Is Celsius, specified in query
			/// </summary>
			[JsonPropertyName("temperature_2m")] public List<double> Temperature { get; set; } = new();
			/// <summary>
			/// Is Celsius, specified in query
			/// </summary>
			[JsonPropertyName("apparent_temperature")] public List<double> ApparentTemperature { get; set; } = new();
			[JsonPropertyName("weather_code")] public List<int> WeatherCode { get; set; } = new();
			[JsonPropertyName("rain")] public List<double> Rain { get; set; } = new();
			[JsonPropertyName("snowfall")] public List<double> Snowfall { get; set; } = new();
			[JsonPropertyName("is_day")] public List<double> IsDay { get; set; } = new();
		}
		#endregion
	}
}
