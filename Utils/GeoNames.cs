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

		public static bool IsLoading { get; private set; } = false;
		public static bool IsLoaded => allGeoNamesLocations != null && allGeoNamesLocations.Count > 0;
		public static List<GeoNamesLocation> AllGeoNamesLocations => allGeoNamesLocations;

		internal static void Load()
		{
			IsLoading = true;

			string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "cities15000.txt");

			allGeoNamesLocations = new();

			Debug.WriteLine($"Loading GeoNames locations from {path}...");

			foreach (var line in File.ReadLines(path))
			{
				var parts = line.Split('\t');
				if (parts.Length < 9) continue;

				string name = parts[1]; // name of geographical point (utf8)
				string nameASCII = parts[2]; // name of geographical point in plain ascii characters, varchar(200)
				string alternatenames = parts[3]; // alternatenames, comma separated, ascii names automatically transliterated
				string country = parts[8]; // country code - ISO-3166 2-letter country code, 2 characters
				string admin1Code = parts[10]; // fipscode (subject to change to iso code)
				double latitude = double.Parse(parts[4], CultureInfo.InvariantCulture);
				double longitude = double.Parse(parts[5], CultureInfo.InvariantCulture);
				int population = int.Parse(parts[14], CultureInfo.InvariantCulture);

				allGeoNamesLocations.Add(new GeoNamesLocation(name, nameASCII, alternatenames, country, admin1Code, latitude, longitude, population));
			}

			Debug.WriteLine($"Loaded {allGeoNamesLocations.Count} locations from {path}");
			IsLoading = false;
		}

		public record GeoNamesLocation(string Name, string NameASCII, string AlternateNames, string Country, string Admin1Code, double Latitude, double Longitude, int Population);
	}
}