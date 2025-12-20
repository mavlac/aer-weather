using System;
using System.Globalization;
using Windows.Storage;

namespace Aer.Data
{
	/// <summary>
	/// The location, used for current forecast, saved in LocalSettings
	/// </summary>
	internal class LocationData
	{
		private const string LocalSettingsKeyPrefix = nameof(LocationData);

		private const string DefaultLocationName = "Prague";
		private const string DefaultLocationCountry = "CZ";
		private const double DefaultLocationLatitude = 50.08804;
		private const double DefaultLocationLongitude = 14.42076;

		private const string LocationLabelFormat = "{0}, {1}"; // Name, Country

		public static string LocationLabel => string.Format(LocationLabelFormat, LocationName, LocationCountry);
		public static string? LocationName { get; private set; }
		public static string? LocationCountry { get; private set; }
		public static double? LocationLatitude { get; private set; }
		public static double? LocationLongitude { get; private set; }
		public static int? LocationID { get; private set; }

		public static string? ReadableLocationCoordinates => LocationLatitude is null || LocationLongitude is null ? null : $"{LocationLatitude.Value.ToString("F4", CultureInfo.InvariantCulture)}, {LocationLongitude.Value.ToString("F4", CultureInfo.InvariantCulture)}";

		public static bool LoadOrSetDefaults()
		{
			var localSettings = ApplicationData.Current.LocalSettings;
			
			// Loading the saved location details
			if (localSettings.Values.TryGetValue($"{LocalSettingsKeyPrefix}_{nameof(LocationName)}", out var locationNameObj) &&
				localSettings.Values.TryGetValue($"{LocalSettingsKeyPrefix}_{nameof(LocationCountry)}", out var locationCountryObj) &&
				localSettings.Values.TryGetValue($"{LocalSettingsKeyPrefix}_{nameof(LocationLatitude)}", out var locationLatitudeObj) &&
				localSettings.Values.TryGetValue($"{LocalSettingsKeyPrefix}_{nameof(LocationLongitude)}", out var locationLongitudeObj) &&
				localSettings.Values.TryGetValue($"{LocalSettingsKeyPrefix}_{nameof(LocationID)}", out var locationIDObj))
			{
				LocationName = (string)locationNameObj;
				LocationCountry = (string)locationCountryObj;
				LocationLatitude = (double)locationLatitudeObj;
				LocationLongitude = (double)locationLongitudeObj;
				LocationID = (int)locationIDObj;
				return true;
			}
			else
			{
				SetLocation(DefaultLocationName, DefaultLocationCountry, DefaultLocationLatitude, DefaultLocationLongitude);
				return false;
			}
		}

		public static void SetLocation(string newLocationName, string newLocationCountry, double newLocationLatitude, double newLocationLongitude)
		{
			LocationName = newLocationName;
			LocationCountry = newLocationCountry;
			LocationLatitude = newLocationLatitude;
			LocationLongitude = newLocationLongitude;
			LocationID = GetLocationId(LocationLatitude.Value, LocationLongitude.Value);

			var localSettings = ApplicationData.Current.LocalSettings;

			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(LocationName)}"] = LocationName;
			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(LocationCountry)}"] = LocationCountry;
			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(LocationLatitude)}"] = LocationLatitude;
			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(LocationLongitude)}"] = LocationLongitude;
			localSettings.Values[$"{LocalSettingsKeyPrefix}_{nameof(LocationID)}"] = LocationID;
		}

		/// <summary>
		/// LocationData Id is a hash calculated from rounded latitude/longitude.
		/// This way locations got from GeoData and from IPInfo can be kind-of compared.
		/// </summary>
		private static int GetLocationId(double latitude, double longitude)
		{
			// Round to avoid floating noise
			var lat = Math.Round(latitude, 3);
			var lon = Math.Round(longitude, 3);

			// Convert to long bits (stable numeric representation)
			long latBits = BitConverter.DoubleToInt64Bits(lat);
			long lonBits = BitConverter.DoubleToInt64Bits(lon);

			// Combine deterministically
			long hash = latBits ^ (lonBits * 31);

			// Compress to int
			return (int)(hash ^ (hash >> 32));
		}
	}
}
