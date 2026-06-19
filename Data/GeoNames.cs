using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Aer.Data
{
	internal static class GeoNames
	{
		private static List<GeoNamesLocation> allGeoNamesLocations = new();

		public static bool IsLoading { get; private set; } = false;
		public static bool IsLoaded => allGeoNamesLocations != null && allGeoNamesLocations.Count > 0;
		public static List<GeoNamesLocation> AllGeoNamesLocations => allGeoNamesLocations;

		internal static async Task Load(System.Threading.CancellationToken cancellationToken = default)
		{
			IsLoading = true;

			// Simulate a long-running operation
			//await Task.Delay(1000);

			await Task.Run(() =>
			{
				string baseDir = AppContext.BaseDirectory;
				string assetsDir = Path.Combine(baseDir, "Assets", "Data");
				string filePath = Path.Combine(assetsDir, "cities500.txt");

				// Validate directory
				if (!Directory.Exists(assetsDir))
				{
					Debug.WriteLine($"GeoNames load failed: Directory not found: {assetsDir}");
					IsLoading = false;
					return;
				}

				// Validate file
				if (!File.Exists(filePath))
				{
					Debug.WriteLine($"GeoNames load failed: File not found: {filePath}");
					IsLoading = false;
					return;
				}

				Debug.WriteLine($"GeoNames loading locations from {filePath}...");
				
				allGeoNamesLocations = new();
				foreach (var line in File.ReadLines(filePath))
				{
					if (cancellationToken.IsCancellationRequested)
						return;

					var parts = line.Split('\t');
					if (parts.Length < 9) continue;

					int id = int.Parse(parts[0], CultureInfo.InvariantCulture); // name of geographical point (utf8)
					string name = parts[1]; // name of geographical point (utf8)
					string nameASCII = parts[2]; // name of geographical point in plain ascii characters, varchar(200)
					string alternatenames = parts[3]; // alternate names, comma separated, ascii names automatically transliterated
					string country = parts[8]; // country code - ISO-3166 2-letter country code, 2 characters
					string admin1Code = parts[10]; // fipscode (subject to change to iso code)
					double latitude = double.Parse(parts[4], CultureInfo.InvariantCulture);
					double longitude = double.Parse(parts[5], CultureInfo.InvariantCulture);
					int population = int.Parse(parts[14], CultureInfo.InvariantCulture);
					
					allGeoNamesLocations.Add(new GeoNamesLocation(id, name, nameASCII, alternatenames, country, admin1Code, latitude, longitude, population));
				}
				
				Debug.WriteLine($"GeoNames loaded {allGeoNamesLocations.Count} locations");
			});
			
			IsLoading = false;
		}

		public record GeoNamesLocation(int ID, string Name, string NameASCII, string AlternateNames, string Country, string Admin1Code, double Latitude, double Longitude, int Population);
	}
}