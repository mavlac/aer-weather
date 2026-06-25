using Microsoft.Data.Sqlite;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace Aer.Data
{
	internal static class WeatherDataCache
	{
		private const string CacheFileName = "cache.db";

		public enum GetWeatherDataResponse
		{
			IsLoaded,
			IsLoadedButExpired,
			NotLoaded
		}

		private static readonly string _dbFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, CacheFileName);

		private static string NowIso() => DateTimeOffset.UtcNow.ToString("O");

		private static SqliteConnection CreateConnection()
		{
			return new SqliteConnection($"Data Source={_dbFilePath}");
		}

		public static void Initialize()
		{
			Directory.CreateDirectory(Path.GetDirectoryName(_dbFilePath)!);
			
			using var connection = CreateConnection();
			connection.Open();
			
			using var cmd = connection.CreateCommand();
			cmd.CommandText = """
				CREATE TABLE IF NOT EXISTS ForecastCache (
				    LocationId     INTEGER NOT NULL,
				    ProviderId     INTEGER NOT NULL,
				    CreatedUtc     TEXT NOT NULL,
				    ValidUntilUtc  TEXT NOT NULL,
				    Data           TEXT NOT NULL,
				    PRIMARY KEY (LocationId, ProviderId)
				);
				""";
			
			cmd.ExecuteNonQuery();
		}

		/// <summary>
		/// Delete expired records from the cache,
		/// while keeping the current location/provider record even if expired,
		/// so it can be loaded and shown on app startup, before new data is fetched.
		/// Connection may be slow or unavailable.
		/// </summary>
		public static void CleanupRecords()
		{
			if (App.IsShuttingDown)
				return;

			int? currentLocationID = LocationManager.CurrentLocation?.ID;
			int? currentProviderID = Preferences.WeatherProviderId;

			using var connection = CreateConnection();
			connection.Open();

			using var cmd = connection.CreateCommand();
			if (currentLocationID.HasValue && currentProviderID.HasValue)
			{
				// Delete expired records but keep the cache entry for the currently selected location/provider even if expired.
				cmd.CommandText = """
					DELETE FROM ForecastCache
					WHERE ValidUntilUtc <= @now
					  AND NOT (LocationId = @keepLoc AND ProviderId = @keepProv);
					""";
				cmd.Parameters.AddWithValue("@keepLoc", currentLocationID.Value);
				cmd.Parameters.AddWithValue("@keepProv", currentProviderID.Value);
			}
			else
			{
				// Delete all expired records.
				cmd.CommandText = """
					DELETE FROM ForecastCache
					WHERE ValidUntilUtc <= @now;
					""";
			}
			cmd.Parameters.AddWithValue("@now", NowIso());
			int deletedRecords = cmd.ExecuteNonQuery();
			
			if (deletedRecords > 0)
			{
				Debug.WriteLine($"WeatherDataCache cleanup removed {deletedRecords} expired records.");
			}
		}

		private static void DeleteSingleEntry(int locationID, int providerID)
		{
			try
			{
				using var connection = CreateConnection();
				connection.Open();
				
				using var cmd = connection.CreateCommand();
				cmd.CommandText = """
					DELETE FROM ForecastCache
					WHERE LocationId = @locId
					  AND ProviderId = @provId;
					""";
				cmd.Parameters.AddWithValue("@locId", locationID);
				cmd.Parameters.AddWithValue("@provId", providerID);
				cmd.ExecuteNonQuery();
			}
			catch
			{
				// Ignore cache maintenance failures
			}
		}

		/// <summary>
		/// Tries to load weather data from the cache for the specified location and provider.
		/// Loaded data may be expired, in which case the caller should fetch new data from the provider and update the cache.
		/// Expired data is still returned so the app can display it while waiting for new data to be fetched.
		/// Corrupted cache entries are removed and treated as not loaded.
		/// </summary>
		public static GetWeatherDataResponse GetWeatherData(int locationID, int weatherProviderID, out WeatherData? weatherData)
		{
			if (App.IsShuttingDown)
			{
				weatherData = null;
				return GetWeatherDataResponse.NotLoaded;
			}

			using var connection = CreateConnection();
			connection.Open();

			using var cmd = connection.CreateCommand();
			cmd.CommandText = """
				SELECT Data, CASE WHEN ValidUntilUtc <= @now THEN 1 ELSE 0 END AS IsExpired
				FROM ForecastCache
				WHERE LocationId = @locId
				  AND ProviderId = @provId;
				""";
			cmd.Parameters.AddWithValue("@locId", locationID);
			cmd.Parameters.AddWithValue("@provId", weatherProviderID);
			cmd.Parameters.AddWithValue("@now", NowIso());

			using var reader = cmd.ExecuteReader();
			if (!reader.Read())
			{
				weatherData = null;
				return GetWeatherDataResponse.NotLoaded;
			}
			string json = reader.GetString(0);
			bool isExpired = reader.GetBoolean(1);

			try
			{
				// Attempt to deserialize the JSON data into a WeatherData object
				weatherData = JsonSerializer.Deserialize<WeatherData>(json);
				if (weatherData != null)
				{
					return isExpired ? GetWeatherDataResponse.IsLoadedButExpired : GetWeatherDataResponse.IsLoaded;
				}
				else
				{
					return GetWeatherDataResponse.NotLoaded;
				}
			}
			catch
			{
				// Remove corrupted entry
				DeleteSingleEntry(locationID, weatherProviderID);

				weatherData = null;
				return GetWeatherDataResponse.NotLoaded;
			}
		}

		public static void SaveWeatherData(WeatherData weatherData)
		{
			if (App.IsShuttingDown)
				return;
			
			try
			{
				using var connection = CreateConnection();
				connection.Open();

				var json = JsonSerializer.Serialize(weatherData);

				using var cmd = connection.CreateCommand();

				cmd.CommandText = """
					INSERT INTO ForecastCache
					(LocationId, ProviderId, CreatedUtc, ValidUntilUtc, Data)
					VALUES
					(@locId, @provId, @created, @valid, @data)
					ON CONFLICT(LocationId, ProviderId)
					DO UPDATE SET
						CreatedUtc    = excluded.CreatedUtc,
						ValidUntilUtc = excluded.ValidUntilUtc,
						Data          = excluded.Data;
					""";

				cmd.Parameters.AddWithValue("@locId", weatherData.LocationID);
				cmd.Parameters.AddWithValue("@provId", weatherData.WeatherProviderID);
				cmd.Parameters.AddWithValue("@created", weatherData.Created.ToString("O"));
				cmd.Parameters.AddWithValue("@valid", weatherData.ValidUntil.ToString("O"));
				cmd.Parameters.AddWithValue("@data", json);

				cmd.ExecuteNonQuery();
			}
			catch (SqliteException)
			{
				// DB corruption or write failure → reset cache
				ResetCache();
			}
		}

		public static void ResetCache()
		{
			if (App.IsShuttingDown)
				return;
			
			try
			{
				using var connection = CreateConnection();
				connection.Open();
				
				using var cmd = connection.CreateCommand();
				cmd.CommandText = "DROP TABLE IF EXISTS ForecastCache;";
				cmd.ExecuteNonQuery();
				
				Initialize();
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Cache reset failed: {ex.Message}");
			}
		}

		public static (int Total, int Valid, int Expired) GetStatistics()
		{
			using var connection = CreateConnection();
			connection.Open();
			
			using var cmd = connection.CreateCommand();
			cmd.CommandText = """
				SELECT
					COUNT(*) AS TotalCount,
					SUM(CASE WHEN ValidUntilUtc > @now THEN 1 ELSE 0 END) AS ValidCount
				FROM ForecastCache;
				""";
			cmd.Parameters.AddWithValue("@now", NowIso());
			
			using var reader = cmd.ExecuteReader();
			
			if (!reader.Read())
				return default;
			
			int total = reader.GetInt32(0);
			int valid = reader.IsDBNull(1)
				? 0
				: reader.GetInt32(1);
			int expired = total - valid;
			
			return (Total: total, Valid: valid, Expired: expired);
		}
	}
}
