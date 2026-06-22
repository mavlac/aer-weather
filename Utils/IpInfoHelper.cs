using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Aer.Utils
{
	public static class IpInfoHelper
	{
		private const string IpInfoUrl = "https://ipinfo.io/json";
		private const double NetworkTimeoutSeconds = 5.0;

		private static HttpClient? _httpClient;

		public record IpInfoLocationResponse
		{
			public string? City;
			public string? Country;
			public double Latitude;
			public double Longitude;
		}

		public static async Task<IpInfoLocationResponse?> GetLocationAsync()
		{
			try
			{
				_httpClient ??= new HttpClient
				{
					Timeout = TimeSpan.FromSeconds(NetworkTimeoutSeconds)
				};

				var response = await _httpClient.GetStringAsync(IpInfoUrl);
				using var doc = JsonDocument.Parse(response);
				var root = doc.RootElement;

				string city = root.GetProperty("city").ToString();
				string country = root.GetProperty("country").ToString();
				string loc = root.GetProperty("loc").ToString();

				double latitude = 0, longitude = 0;
				var parts = loc.Split(',');
				if (parts.Length == 2 &&
					double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat) &&
					double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lon))
				{
					latitude = lat;
					longitude = lon;
				}

				return new IpInfoLocationResponse
				{
					City = city,
					Country = country,
					Latitude = latitude,
					Longitude = longitude
				};
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to get IP info: " + ex.Message);

				await new Microsoft.UI.Xaml.Controls.ContentDialog
				{
					Title = "Location Error",
					Content = $"Unable to retrieve location information.\n\n{ex.Message}",
					CloseButtonText = "OK",
					XamlRoot = App.MainWindow.Content.XamlRoot
				}.ShowAsync();

				return null;
			}
		}
	}
}
