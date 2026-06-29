using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Windows.Storage;

namespace Aer.Data
{
	/// <summary>
	/// The user-selected current location.
	/// Saved in LocalSettings
	/// </summary>
	internal static class LocationManager
	{
		private const int MaxRecentLocations = 5;

		private const string LocalSettingsKeyPrefix = nameof(LocationManager);

		private const string DefaultName = "Prague";
		private const string DefaultCountryCode = "CZ";
		private const double DefaultLatitude = 50.08804;
		private const double DefaultLongitude = 14.42076;

		private static List<Location> recentLocations = [];

		public static Location? CurrentLocation => recentLocations.LastOrDefault();
		public static List<Location> RecentLocations => recentLocations;

		/// <summary>
		/// Clears the current location and recent locations from LocalSettings.
		/// </summary>
		public static void ClearRecents()
		{
			var localSettings = ApplicationData.Current.LocalSettings;
			
			recentLocations.Clear();
			localSettings.Values.Remove($"{LocalSettingsKeyPrefix}_{nameof(recentLocations)}");
		}

		/// <summary>
		/// Loads the location from LocalSettings. If not found, sets the default location.
		/// </summary>
		public static bool Load()
		{
			var localSettings = ApplicationData.Current.LocalSettings;

			// Primary loading
			bool isJsonLoadedAndDeserialized = false;
			if (localSettings.Values.TryGetValue($"{LocalSettingsKeyPrefix}_{nameof(recentLocations)}", out var value) && value is string json)
			{
				try
				{
					recentLocations = JsonSerializer.Deserialize<List<Location>>(json) ?? [];
					// At least one record must be present to consider the JSON deserialization successful
					if (recentLocations.Count > 0)
					{
						isJsonLoadedAndDeserialized = true;
					}
				}
				catch (JsonException)
				{
					// Ignore JSON deserialization errors
					Debug.WriteLine("Error deserializing recent locations from LocalSettings.");
				}
			}

			if (isJsonLoadedAndDeserialized)
			{
				// Successfully loaded from saved JSON
				return true;
			}
			else if (TryGetObsoleteFormatValues(out string name, out string countryCode, out double latitude, out double longitude))
			{
				// Fallback 1) Try loading from the original separate-value format
				// TODO: DELETE when it is sure everyone safely updated
				Debug.WriteLine("Current location loaded using fallback original separate-value format.");
				Set(name, countryCode, latitude, longitude);
				return true;
			}
			else
			{
				// Fallback 2) Set defaults
				Debug.WriteLine("Unable to load current location - setting default.");
				Set(DefaultName, DefaultCountryCode, DefaultLatitude, DefaultLongitude);
				return false;
			}
		}

		/// <summary>
		/// Sets the current location and saves it to LocalSettings.
		/// </summary>
		public static void Set(string newLocationName, string newLocationCountryCode, double newLocationLatitude, double newLocationLongitude)
		{
			var location = new Location(
				GetLocationID(newLocationLatitude, newLocationLongitude),
				newLocationName,
				newLocationCountryCode,
				newLocationLatitude,
				newLocationLongitude);

			Set(location);
		}

		/// <summary>
		/// Sets the current location and saves it to LocalSettings.
		/// </summary>
		public static void Set(Location location)
		{
			// No location change, skip setting and saving
			if (CurrentLocation?.ID == location.ID)
				return;

			// Remove existing occurrence
			recentLocations.RemoveAll(x => x.ID == location.ID);

			// Add as most recent
			recentLocations.Add(location);

			// Keep only newest entries
			while (recentLocations.Count > MaxRecentLocations)
			{
				recentLocations.RemoveAt(0);
			}

			var localSettings = ApplicationData.Current.LocalSettings;

			var json = JsonSerializer.Serialize(recentLocations);
			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(recentLocations)}"] = json;
		}

		/// <summary>
		/// LocationManager ID is a hash calculated from rounded latitude/longitude.
		/// This way locations got from GeoData and from IPInfo can be kind-of compared.
		/// </summary>
		private static int GetLocationID(double latitude, double longitude)
		{
			// Round to avoid floating noise
			double roundLatitude = Math.Round(latitude, 3);
			double roundLongitude = Math.Round(longitude, 3);
			
			// Convert to long bits (stable numeric representation)
			long latBits = BitConverter.DoubleToInt64Bits(roundLatitude);
			long lonBits = BitConverter.DoubleToInt64Bits(roundLongitude);
			
			// Combine deterministically
			long hash = latBits ^ (lonBits * 31);
			
			// Compress to int
			return (int)(hash ^ (hash >> 32));
		}

		/// <summary>
		/// Tries to load the location from the original separate-value format (before we switched to storing the whole Location record List).
		/// TODO: DELETE when it is sure everyone safely updated
		/// </summary>
		private static bool TryGetObsoleteFormatValues(out string name, out string countryCode, out double latitude, out double longitude)
		{
			var localSettings = ApplicationData.Current.LocalSettings;

			const string obsoleteLocalSettingsKeyPrefix = "Location";
			// Loading the obsolete format saved location details
			if (localSettings.Values.TryGetValue($"{obsoleteLocalSettingsKeyPrefix}_Country", out var locationCountryCodeObj) &&
				localSettings.Values.TryGetValue($"{obsoleteLocalSettingsKeyPrefix}_Latitude", out var locationLatitudeObj) &&
				localSettings.Values.TryGetValue($"{obsoleteLocalSettingsKeyPrefix}_Longitude", out var locationLongitudeObj) &&
				localSettings.Values.TryGetValue($"{obsoleteLocalSettingsKeyPrefix}_Name", out var locationNameObj))
			{
				name = (string)locationNameObj;
				countryCode = (string)locationCountryCodeObj;
				latitude = (double)locationLatitudeObj;
				longitude = (double)locationLongitudeObj;
				return true;
			}
			else
			{
				name = string.Empty;
				countryCode = string.Empty;
				latitude = 0d;
				longitude = 0d;
				return false;
			}
		}



		public class Location(int ID, string Name, string CountryCode, double Latitude, double Longitude)
		{
			public int ID { get; } = ID;
			public string Name { get; } = Name;
			public string CountryCode { get; } = CountryCode;
			public double Latitude { get; } = Latitude;
			public double Longitude { get; } = Longitude;

			public string Label => $"{Name}, {CountryCode}";
			public string? ReadableCoordinates => $"{Latitude.ToString("F4", CultureInfo.InvariantCulture)}, {Longitude.ToString("F4", CultureInfo.InvariantCulture)}";

		}
	}
}
