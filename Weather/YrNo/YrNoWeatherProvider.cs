using Aer.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Aer.Weather.YrNo
{
	public class YrNoWeatherProvider : WeatherProvider
	{
		public const int ProviderStaticId = 1;

		public override int ProviderId => ProviderStaticId;
		public override string ProviderName => "Yr MET/NRK Norway";
		public override string ProviderDescription => "Europe-focused";

		public override async Task<(WeatherResult? weatherResult, string errorMessage)> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken)
		{
			var (yrResponse, expires, errorMessage) = await QueryYrNoAsync(latitude, longitude, cancellationToken);
			if (yrResponse == null ||
				yrResponse.Properties == null ||
				yrResponse.Properties.Timeseries == null ||
				expires == null)
			{
				return (null, errorMessage ?? "Response parsed incomplete");
			}

			try
			{
				// Map current
				var first = yrResponse.Properties?.Timeseries?[0];
				var instant = first?.Data?.Instant?.Details;

				if (instant == null)
					return (null, "Yr.no: Missing 'instant' weather data.");

				var symbol = first.Data.Next1Hours?.Summary?.SymbolCode;

				var current = new CurrentWeather
				{
					Temperature = instant.AirTemperature,
					ApparentTemperature = instant.AirTemperature, // TODO: Yr has no separate apparent temperature in compact API
					WeatherCode = GetWeatherCodeFromSymbol(symbol),
					IsDaytime = GetIsDaytimeFromSymbol(symbol)
				};

				// Map hourly
				var hourly = new List<HourlyForecast>();

				foreach (var ts in yrResponse.Properties.Timeseries)
				{
					var hourInstant = ts.Data?.Instant?.Details;
					if (hourInstant == null)
						continue;

					var sym = ts.Data.Next1Hours?.Summary?.SymbolCode;

					hourly.Add(new HourlyForecast
					{
						Time = DateTimeUtils.ConvertUtcIsoToLocal(ts.Time),
						Temperature = hourInstant.AirTemperature,
						WeatherCode = GetWeatherCodeFromSymbol(sym),
						IsDaytime = GetIsDaytimeFromSymbol(sym),
						Rain = ts.Data.Next1Hours?.Details?.PrecipitationAmount ?? 0,
						Snowfall = 0 // Yr.no compact API does not expose separate snowfall here
					});
				}

				// Validity (from HTTP header)
				var validUntil = (DateTimeOffset)expires;

				return (new WeatherResult(current, hourly, validUntil), string.Empty);
			}
			catch (Exception ex)
			{
				return (null, $"Yr.no mapping error: {ex.Message}");
			}
		}

		/// <summary>
		/// The network call
		/// </summary>
		private async Task<(YrResponse? yrResponse, DateTimeOffset? expires, string errorMessage)> QueryYrNoAsync(double latitude, double longitude, CancellationToken cancellationToken)
		{
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				
				string url =
					$"https://api.met.no/weatherapi/locationforecast/2.0/compact?" +
					$"lat={latitude.ToString("F4", CultureInfo.InvariantCulture)}&" +
					$"lon={longitude.ToString("F4", CultureInfo.InvariantCulture)}";
				var response = await _httpClient.GetAsync(url, cancellationToken);

				var expires = response.Content.Headers.Expires;
				string json = await response.Content.ReadAsStringAsync(cancellationToken);

				var yrResponse = JsonSerializer.Deserialize<YrResponse>(json, _jsonOptions);
				return (yrResponse, expires, string.Empty);
			}
			catch (OperationCanceledException)
			{
				return (null, null, "Operation cancelled");
			}
			catch (HttpRequestException ex)
			{
				return (null, null, $"Network error: {ex.Message}");
			}
			catch (Exception ex)
			{
				return (null, null, $"Unexpected Yr.no API error: {ex.Message}");
			}
		}

		private static int GetWeatherCodeFromSymbol(string? symbol)
		{
			if (string.IsNullOrEmpty(symbol))
				return 0;

			// Start simple – you can refine mapping later
			if (symbol.StartsWith("clearsky"))
				return 0;
			if (symbol.Contains("cloudy"))
				return 1;
			if (symbol.Contains("rain"))
				return 2;
			if (symbol.Contains("snow"))
				return 3;

			return 99;
		}

		private static bool GetIsDaytimeFromSymbol(string? symbol)
		{
			if (symbol == null) return true;
			return symbol.EndsWith("_day");
		}

		#region Yr.no JSON Models
		public class YrResponse
		{
			[JsonPropertyName("properties")] public YrProperties? Properties { get; set; }
		}

		public class YrProperties
		{
			[JsonPropertyName("timeseries")] public List<YrTimeseries> Timeseries { get; set; } = new();
		}

		public class YrTimeseries
		{
			[JsonPropertyName("time")] public string Time { get; set; } = string.Empty;
			[JsonPropertyName("data")] public YrData? Data { get; set; }
		}

		public class YrData
		{
			[JsonPropertyName("instant")] public YrInstant? Instant { get; set; }
			[JsonPropertyName("next_1_hours")] public YrForecastStep? Next1Hours { get; set; }
			[JsonPropertyName("next_6_hours")] public YrForecastStep? Next6Hours { get; set; }
			[JsonPropertyName("next_12_hours")] public YrForecastStep? Next12Hours { get; set; }
		}

		public class YrInstant
		{
			[JsonPropertyName("details")] public YrDetails? Details { get; set; }
		}

		public class YrDetails
		{
			[JsonPropertyName("air_temperature")] public double AirTemperature { get; set; }
		}

		public class YrForecastStep
		{
			[JsonPropertyName("summary")] public YrSummary? Summary { get; set; }
			[JsonPropertyName("details")] public YrForecastDetails? Details { get; set; }
		}

		public class YrSummary
		{
			[JsonPropertyName("symbol_code")] public string? SymbolCode { get; set; }
		}

		public class YrForecastDetails
		{
			[JsonPropertyName("precipitation_amount")] public double PrecipitationAmount { get; set; }
		}
		#endregion
	}
}
