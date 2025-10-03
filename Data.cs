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

		private static string SettingsPrefix => nameof(Data);

		public static bool IsValid { get; private set; }
		public static string? LocationName { get; private set; }

		public static event Action Updated;

		public static void Save()
		{
			if (!IsValid) throw new InvalidOperationException("Data must be loaded before it can be saved.");

			var settings = ApplicationData.Current.LocalSettings;

			settings.Values[$"{SettingsPrefix}_LocationName"] = LocationName;
		}

		public static void LoadLastSavedValues()
		{
			// Will load from cached data if available

			var settings = ApplicationData.Current.LocalSettings;

			if (settings.Values.TryGetValue($"{SettingsPrefix}_LocationName", out var locationNameObj))
			{
				LocationName = (string)locationNameObj;
				IsValid = true;
			}
			else
			{
				LocationName = DefaultLocationName;
				IsValid = false;
			}
		}

		public static void UpdateFromNetworkDataProvider()
		{
			// TODO: implement loading from data provider

			// Success
			IsValid = true;
			Updated?.Invoke();
		}
	}
}
