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

		public static void CleanupExpiredRecords()
		{
			using var connection = CreateConnection();
			connection.Open();
			
			using var cmd = connection.CreateCommand();
			cmd.CommandText = """
				DELETE FROM ForecastCache
				WHERE ValidUntilUtc <= @now;
				""";
			cmd.Parameters.AddWithValue("@now", NowIso());
			int deletedRecords = cmd.ExecuteNonQuery();
			
			if (deletedRecords > 0)
			{
				Debug.WriteLine($"WeatherDataCache cleanup removed {deletedRecords} expired records.");
			}
		}

		private static void DeleteSingleEntry(int locationId, int providerId)
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
				cmd.Parameters.AddWithValue("@locId", locationId);
				cmd.Parameters.AddWithValue("@provId", providerId);
				cmd.ExecuteNonQuery();
			}
			catch
			{
				// Ignore cache maintenance failures
			}
		}

		public static bool GetWeatherData(int locationID, int weatherProviderID, out WeatherData? weatherData)
		{
			using var connection = CreateConnection();
			connection.Open();
			
			using var cmd = connection.CreateCommand();
			cmd.CommandText = """
				SELECT Data
				FROM ForecastCache
				WHERE LocationId = @locId
				  AND ProviderId = @provId
				  AND ValidUntilUtc > @now;
				""";
			cmd.Parameters.AddWithValue("@locId", locationID);
			cmd.Parameters.AddWithValue("@provId", weatherProviderID);
			cmd.Parameters.AddWithValue("@now", NowIso());
			
			var result = cmd.ExecuteScalar();
			
			if (result is not string json)
			{
				weatherData = null;
				return false;
			}

			try
			{
				weatherData = JsonSerializer.Deserialize<WeatherData>(json);
				return weatherData != null;
			}
			catch
			{
				// Remove corrupted entry
				DeleteSingleEntry(locationID, weatherProviderID);
				
				weatherData = null;
				return false;
			}
		}

		public static void SaveWeatherData(WeatherData weatherData)
		{
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
