using Aer.Weather.OpenMeteo;
using Aer.Weather.YrNo;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
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
		private const double NetworkTimeoutSeconds = 10.0;

		private static readonly HttpClient _sharedClient = CreateDefaultClient();
		protected readonly HttpClient _httpClient;

		public abstract int ProviderId { get; }
		public abstract string ProviderName { get; }

		// Hand-maintain this Dictionary of weather providers, they do not subscribe dynamically
		private static readonly Dictionary<int, WeatherProvider> _providers = new()
		{
			{ OpenMeteoWeatherProvider.ProviderStaticId, new OpenMeteoWeatherProvider() },
			{ YrNoWeatherProvider.ProviderStaticId,      new YrNoWeatherProvider() }
		};

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

			string userAgent = $"{Package.Current.DisplayName}/{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build} ({Application.Current.Resources["GitHubRepositoryUrl"]})";
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

		// TODO: All values for dropdown in settings
		public static Dictionary<int, string> GetAllProviderNames()
		{
			return _providers.ToDictionary(
				keyValuePair => keyValuePair.Key,
				keyValuePair => keyValuePair.Value.ProviderName);
		}

		/// <summary>
		/// Fetch current weather and hourly forecast in a single call.
		/// </summary>
		public abstract Task<(WeatherResult? weatherResult, string errorMessage)> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken);
	}
}
