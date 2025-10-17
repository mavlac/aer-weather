using System.Threading;
using System.Threading.Tasks;

namespace Aer.Weather
{
	public interface IWeatherProvider
	{
		/// <summary>
		/// Fetch current weather and hourly forecast in a single call.
		/// </summary>
		Task<WeatherResult?> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken);
	}
}
