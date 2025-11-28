using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aer.Weather.YrNo
{
	public class YrNoWeatherProvider : WeatherProvider
	{
		public override int ProviderId => 1;

		public override async Task<WeatherResult?> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
