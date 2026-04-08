using Aer.Weather.OpenMeteo;
using Aer.Weather.YrNo;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Aer.Weather
{
	/// <summary>
	/// Base class for weather providers.
	/// </summary>
	public abstract class WeatherProvider
	{
		protected const double NetworkTimeoutSeconds = 30.0;
		protected const int DefaultCacheValidityMinutes = 30; // Used when provider does not return validity in the data

		private static readonly HttpClient _sharedClient = CreateDefaultClient();
		protected readonly HttpClient _httpClient;

		// Hand-maintain this Dictionary of weather providers, they do not subscribe dynamically
		private static readonly Dictionary<int, WeatherProvider> _providers = new()
		{
			{
				OpenMeteoWeatherProvider.ProviderStaticId,
				new OpenMeteoWeatherProvider()
			},
			{
				YrNoWeatherProvider.ProviderStaticId,
				new YrNoWeatherProvider()
			}
		};

		protected readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

		public WeatherProvider()
		{
			_httpClient = _sharedClient;
		}

		private static HttpClient CreateDefaultClient()
		{
			var client = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(NetworkTimeoutSeconds)
			};

			string userAgent = $"{Package.Current.DisplayName}/{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build} ({Application.Current.Resources["GitHubRepositoryURL"]})";
			client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

			Debug.WriteLine($"Created shared HttpClient with user agent: {client.DefaultRequestHeaders.UserAgent}");

			return client;
		}

		/// <summary>
		/// Returns the preferred weather provider's ID, that is used as a default on application first start.
		/// </summary>
		public static int GetPreferredProviderId()
		{
			return OpenMeteoWeatherProvider.ProviderStaticId;
		}

		public static WeatherProvider Get(int id)
		{
			if (_providers.TryGetValue(id, out var provider))
				return provider;
			
			throw new ArgumentException($"Unknown weather provider with Id: {id}");
		}

		public static Dictionary<int, string> GetAllProvidersForUserSelection()
		{
			return
				_providers.ToDictionary(
					keyValuePair => keyValuePair.Key,
					keyValuePair => $"{keyValuePair.Value.ProviderName} ({keyValuePair.Value.ProviderDescription})");
		}



		public abstract int ProviderId { get; }
		public abstract string ProviderName { get; }
		public abstract string ProviderURL { get; }
		public abstract string ProviderDescription { get; }

		/// <summary>
		/// Fetch current weather and hourly forecast in a single call.
		/// This is the API a provider must implement.
		/// </summary>
		public abstract Task<(WeatherResult? weatherResult, string errorMessage)> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken);
	}
}
