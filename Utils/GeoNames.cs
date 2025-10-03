using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Aer.Utils
{
	internal static class GeoNames
	{
		private static List<GeoNamesLocation> allGeoNamesLocations = new();

		public static bool IsLoaded => allGeoNamesLocations != null && allGeoNamesLocations.Count > 0;
		public static List<GeoNamesLocation> AllGeoNamesLocations => allGeoNamesLocations;

		internal static void Load()
		{
			string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "cities15000.txt");

			allGeoNamesLocations = new();

			Debug.WriteLine($"Loading GeoNames locations from {path}...");

			foreach (var line in File.ReadLines(path))
			{
				var parts = line.Split('\t');
				if (parts.Length < 9) continue;

				string name = parts[1]; // city name
				string country = parts[8]; // country code
				double latitude = double.Parse(parts[4], CultureInfo.InvariantCulture);
				double longitude = double.Parse(parts[5], CultureInfo.InvariantCulture);

				allGeoNamesLocations.Add(new GeoNamesLocation(name, country, latitude, longitude));
			}

			Debug.WriteLine($"Loaded {allGeoNamesLocations.Count} locations from {path}");
		}

		public record GeoNamesLocation(string City, string Country, double Latitude, double Longitude);
	}
}