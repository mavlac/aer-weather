using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aer.Weather
{
	public class HourlyForecast
	{
		public string Time { get; set; } = string.Empty;
		public double Temperature { get; set; }
		public int ConditionCode { get; set; }
		public double Rain { get; set; }
		public double Snowfall { get; set; }

		public string ConditionText => WeatherDescriptions.GetDescription(ConditionCode);
	}
}
