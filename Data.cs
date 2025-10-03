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
		private const string DefaultLastUpdateTime = "—";

		private static string SettingsPrefix => nameof(Data);

		public static bool IsValid { get; private set; }
		public static string? Condition { get; private set; }
		public static string? Temperature { get; private set; }
		public static string? LocationName { get; private set; }
		public static string? LastUpdateTime { get; private set; }

		public static event Action? Updated;

		public static void LoadLastSavedValues()
		{
			// Will load from cached data if available

			var settings = ApplicationData.Current.LocalSettings;

			if (settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(LocationName)}", out var locationNameObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(Condition)}", out var conditionNameObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(Temperature)}", out var temperatureNameObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_{nameof(LastUpdateTime)}", out var lastUpdateTimeObj))
			{
				LocationName = (string)locationNameObj;
				Condition = (string)conditionNameObj;
				Temperature = (string)temperatureNameObj;
				LastUpdateTime = (string)lastUpdateTimeObj;
				IsValid = true;
			}
			else
			{
				LocationName = DefaultLocationName;
				LastUpdateTime = DefaultLastUpdateTime;
				IsValid = false;
			}
		}

		public static void UpdateFromNetworkDataProvider()
		{
			// TODO: implement loading from data provider

			// TODO: On Success
			//IsValid = true;
			//Updated?.Invoke();

			//Save();
		}

		public static void Save()
		{
			if (!IsValid) throw new InvalidOperationException("Data must be loaded before it can be saved.");

			var settings = ApplicationData.Current.LocalSettings;

			settings.Values[$"{SettingsPrefix}_{nameof(LocationName)}"] = LocationName;
			settings.Values[$"{SettingsPrefix}_{nameof(Condition)}"] = Condition;
			settings.Values[$"{SettingsPrefix}_{nameof(Temperature)}"] = Temperature;
			settings.Values[$"{SettingsPrefix}_{nameof(LastUpdateTime)}"] = LastUpdateTime;
		}
	}
}
