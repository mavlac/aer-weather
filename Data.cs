using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Aer
{
	public static class Data
	{
		private const string DefaultLocationName = "Prague";

		public static string? LocationName { get; private set; }

		public static void Initialize()
		{
			// Will load from cached data if available

			var settings = ApplicationData.Current.LocalSettings;
			var prefix = nameof(Data);

			if (settings.Values.TryGetValue($"{prefix}_LocationName", out var locationNameObj))
			{
				LocationName = (string)locationNameObj;
			}
			else
			{
				LocationName = DefaultLocationName;
			}
		}
	}
}
