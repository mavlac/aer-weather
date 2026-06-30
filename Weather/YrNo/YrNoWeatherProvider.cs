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

namespace Aer.Weather.YrNo
{
	/// <summary>
	/// Weather provider for Yr.no API (https://api.met.no/weatherapi/locationforecast/2.0/documentation).
	/// </summary>
	public partial class YrNoWeatherProvider : WeatherProvider
	{
		public const int ProviderStaticId = 1;

		public override int ProviderId => ProviderStaticId;
		public override string ProviderName => "Yr MET/NRK Norway";
		public override string ProviderURL => "api.met.no";
		public override string ProviderDescription => "Europe-focused";

		public override async Task<(WeatherResult? weatherResult, string errorMessage)> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken)
		{
			var (yrNoResponse, expires, errorMessage) = await QueryYrNoAsync(latitude, longitude, cancellationToken);
			if (yrNoResponse == null ||
				yrNoResponse.Properties == null ||
				yrNoResponse.Properties.Timeseries == null ||
				yrNoResponse.Properties.Timeseries.Count == 0 ||
				expires == null)
			{
				return (null, errorMessage ?? "Response parsed incomplete");
			}

			try
			{
				// Map current
				var first = yrNoResponse.Properties.Timeseries[0];
				var firstDataInstantDetails = first?.Data?.Instant?.Details;
				string? firstSymbol = first?.Data?.Next1Hours?.Summary?.SymbolCode;
				
				Debug.WriteLine(first + " " + firstDataInstantDetails + " " + firstSymbol);

				if (first == null || firstDataInstantDetails == null || string.IsNullOrEmpty(firstSymbol))
				{
					return (null, "Response parsed incomplete: missing first timeseries data");
				}
				
				var current = new CurrentWeather
				{
					Temperature = firstDataInstantDetails.AirTemperature,
					ApparentTemperature = firstDataInstantDetails.AirTemperature, // TODO: Yr has no separate apparent temperature in compact API
					WeatherCode = GetWMOCodeFromYrNoSymbol(firstSymbol),
					IsDaytime = GetIsDaytimeFromYrNoSymbol(firstSymbol)
				};

				// Map hourly
				var hourly = new List<HourlyForecast>();
				
				foreach(var timeSerie in yrNoResponse.Properties.Timeseries)
				{
					var time = timeSerie.Time; // UTC ISO string
					var timeOffset = DateTimeOffset.Parse(time, null, DateTimeStyles.AssumeUniversal);
					var dataInstantDetails = timeSerie.Data?.Instant?.Details;
					string? symbol = timeSerie?.Data?.Next1Hours?.Summary?.SymbolCode;

					// YrNo timeseries are every hour and after ~3 days, they are every 6 hours
					// Only fine sampled data is useful for hourly forecasts, so we skip all data that is more than hour in future from the last remapped hourly record
					if (hourly.Count > 0 && hourly[^1].Time.AddHours(1) < timeOffset.DateTime)
					{
						break;
					}

					// YrNo does not expose separate rain/snow amounts, only total precipitation amount and a symbol code that indicates if it is snow or rain.
					// We split the total precipitation amount into rain/snow based on the symbol code.
					double precipitationAmount = timeSerie?.Data?.Next1Hours?.Details?.PrecipitationAmount ?? 0d;
					bool isPrecipitationSnow = symbol?.Contains("snow", StringComparison.OrdinalIgnoreCase) ?? false;
					double rain = isPrecipitationSnow ? 0d : precipitationAmount;
					double snow = isPrecipitationSnow ? precipitationAmount : 0d;

					hourly.Add(new HourlyForecast
					{
						Time = timeOffset.DateTime,
						IsDaytime = GetIsDaytimeFromYrNoSymbol(symbol),
						Temperature = dataInstantDetails?.AirTemperature ?? 0d,
						WeatherCode = GetWMOCodeFromYrNoSymbol(symbol),
						Rain = rain,
						Snowfall = snow
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
		private async Task<(YrNoResponse? yrNoResponse, DateTimeOffset? expires, string errorMessage)> QueryYrNoAsync(double latitude, double longitude, CancellationToken cancellationToken)
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

				var yrNoResponse = JsonSerializer.Deserialize<YrNoResponse>(json, _jsonOptions);
				return (yrNoResponse, expires, string.Empty);
			}
			catch (OperationCanceledException ex) when (ex is TaskCanceledException)
			{
				// Could be timeout or manual cancellation
				if (!cancellationToken.IsCancellationRequested)
				{
					return (null, null, "Operation timed out");
				}
				else
				{
					return (null, null, "Operation cancelled by user");
				}
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

		public override bool IsFeatureSupported(Feature feature)
		{
			return false;
		}

		#region Yr.no JSON Models
		public class YrNoResponse
		{
			[JsonPropertyName("properties")] public YrNoProperties? Properties { get; set; }
		}

		public class YrNoProperties
		{
			[JsonPropertyName("timeseries")] public List<YrNoTimeseries> Timeseries { get; set; } = [];
		}

		public class YrNoTimeseries
		{
			[JsonPropertyName("time")] public string Time { get; set; } = string.Empty;
			[JsonPropertyName("data")] public YrNoData? Data { get; set; }
		}

		public class YrNoData
		{
			[JsonPropertyName("instant")] public YrNoInstant? Instant { get; set; }
			[JsonPropertyName("next_1_hours")] public YrNoForecastStep? Next1Hours { get; set; }
			[JsonPropertyName("next_6_hours")] public YrNoForecastStep? Next6Hours { get; set; }
			[JsonPropertyName("next_12_hours")] public YrNoForecastStep? Next12Hours { get; set; }
		}

		public class YrNoInstant
		{
			[JsonPropertyName("details")] public YrNoDetails? Details { get; set; }
		}

		public class YrNoDetails
		{
			[JsonPropertyName("air_temperature")] public double AirTemperature { get; set; }
		}

		public class YrNoForecastStep
		{
			[JsonPropertyName("summary")] public YrNoSummary? Summary { get; set; }
			[JsonPropertyName("details")] public YrNoForecastDetails? Details { get; set; }
		}

		public class YrNoSummary
		{
			[JsonPropertyName("symbol_code")] public string? SymbolCode { get; set; }
		}

		public class YrNoForecastDetails
		{
			[JsonPropertyName("precipitation_amount")] public double PrecipitationAmount { get; set; }
		}
		#endregion
	}
}
