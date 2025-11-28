using Aer.Weather.OpenMeteo;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
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

		public static int GetPreferredProviderId()
		{
			return OpenMeteoWeatherProvider.ProviderStaticId;
		}


		/// <summary>
		/// Fetch current weather and hourly forecast in a single call.
		/// </summary>
		public abstract Task<WeatherResult?> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken);
	}
}
