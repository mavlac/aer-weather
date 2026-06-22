using System;
using System.Globalization;
using Windows.Storage;

namespace Aer.Data
{
	/// <summary>
	/// The user-selected current location.
	/// Saved in LocalSettings
	/// </summary>
	internal class Location
	{
		private const string LocalSettingsKeyPrefix = nameof(Location);

		private const string DefaultName = "Prague";
		private const string DefaultCountry = "CZ";
		private const double DefaultLatitude = 50.08804;
		private const double DefaultLongitude = 14.42076;

		private const string LabelFormat = "{0}, {1}"; // Name, Country

		public static int? ID { get; private set; }
		public static string? Name { get; private set; }
		public static string? Country { get; private set; }
		public static double? Latitude { get; private set; }
		public static double? Longitude { get; private set; }

		public static string Label => string.Format(LabelFormat, Name, Country);
		public static string? ReadableCoordinates => Latitude is null || Longitude is null ? null : $"{Latitude.Value.ToString("F4", CultureInfo.InvariantCulture)}, {Longitude.Value.ToString("F4", CultureInfo.InvariantCulture)}";

		/// <summary>
		/// Loads the location from LocalSettings. If not found, sets the default location.
		/// </summary>
		public static bool Load()
		{
			var localSettings = ApplicationData.Current.LocalSettings;
			
			// Loading the saved location details
			if (localSettings.Values.TryGetValue($"{LocalSettingsKeyPrefix}_{nameof(ID)}", out var locationIDObj) &&
				localSettings.Values.TryGetValue($"{LocalSettingsKeyPrefix}_{nameof(Country)}", out var locationCountryObj) &&
				localSettings.Values.TryGetValue($"{LocalSettingsKeyPrefix}_{nameof(Latitude)}", out var locationLatitudeObj) &&
				localSettings.Values.TryGetValue($"{LocalSettingsKeyPrefix}_{nameof(Longitude)}", out var locationLongitudeObj) &&
				localSettings.Values.TryGetValue($"{LocalSettingsKeyPrefix}_{nameof(Name)}", out var locationNameObj))
			{
				ID = (int)locationIDObj;
				Name = (string)locationNameObj;
				Country = (string)locationCountryObj;
				Latitude = (double)locationLatitudeObj;
				Longitude = (double)locationLongitudeObj;
				return true;
			}
			else
			{
				Set(DefaultName, DefaultCountry, DefaultLatitude, DefaultLongitude);
				return false;
			}
		}

		/// <summary>
		/// Sets the current location and saves it to LocalSettings.
		/// </summary>
		public static void Set(string newLocationName, string newLocationCountry, double newLocationLatitude, double newLocationLongitude)
		{
			ID = GetLocationID(newLocationLatitude, newLocationLongitude);
			Name = newLocationName;
			Country = newLocationCountry;
			Latitude = newLocationLatitude;
			Longitude = newLocationLongitude;

			var localSettings = ApplicationData.Current.LocalSettings;

			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(ID)}"] = ID;
			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(Name)}"] = Name;
			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(Country)}"] = Country;
			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(Latitude)}"] = Latitude;
			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(Longitude)}"] = Longitude;
		}

		/// <summary>
		/// Location ID is a hash calculated from rounded latitude/longitude.
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
	}
}
