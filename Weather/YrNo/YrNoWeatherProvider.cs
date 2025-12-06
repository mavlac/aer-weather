using Aer.Weather.OpenMeteo;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aer.Weather.YrNo
{
	public class YrNoWeatherProvider : WeatherProvider
	{
		public const int ProviderStaticId = 1;
		public override int ProviderId => ProviderStaticId;
		public override string ProviderName => "Yr.no";

		public override async Task<(WeatherResult? weatherResult, string errorMessage)> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
