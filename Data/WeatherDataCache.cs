using Microsoft.Data.Sqlite;
using System.IO;
using Windows.Storage;

namespace Aer.Data
{
	internal class WeatherDataCache
	{
		private const string CacheFileName = "cache.db";

		private static readonly string _dbFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, CacheFileName);

		// TODO: Not called
		public void Initialize()
		{
			var connectionString = $"Data Source={_dbFilePath}";

			using var connection = new SqliteConnection(connectionString);
			connection.Open();

			// Create table once
			using var cmd = connection.CreateCommand();
			cmd.CommandText =
			"""
CREATE TABLE IF NOT EXISTS ForecastCache (
    LocationId    TEXT NOT NULL,
    ProviderId    TEXT NOT NULL,
    CreatedUtc    TEXT NOT NULL,
    ValidUntilUtc TEXT NOT NULL,
    Data          TEXT NOT NULL,
    PRIMARY KEY (LocationId, ProviderId)
);
""";
			cmd.ExecuteNonQuery();
		}

		public bool GetWeatherData(int locationID, int weatherProviderID, out WeatherData? weatherData)
		{
			// TODO: Query that deletes all records where validUntil < now
			cmd.CommandText =
"""
DELETE FROM ForecastCache
WHERE ValidUntilUtc <= @now;
""";

			// TODO: Query that selects the record for the location/provider pair

			// No cached data for location/provider pair found
			weatherData = null;
			return false;
		}

		public void SaveWeatherData(WeatherData weatherData)
		{
			// TODO: Query that inserts or updates the record for the location/provider pair
		}
	}
}
