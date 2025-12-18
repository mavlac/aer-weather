using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace Aer.Utils
{
	/// <summary>
	/// Nav Pane state, open or closed, saved in LocalSettings
	/// </summary>
	class NavigationViewStateManager
	{
		private const string LocalSettingsKeyPrefix = nameof(NavigationViewStateManager);
		private const string SettingsName = $"{nameof(NavigationView)}{nameof(NavigationView.IsPaneOpen)}";

		public static void Save(NavigationView navigationView)
		{
			var settings = ApplicationData.Current.LocalSettings;

			settings.Values[$"{LocalSettingsKeyPrefix}_{SettingsName}"] = navigationView.IsPaneOpen;
		}

		public static void Restore(NavigationView navigationView, bool defaultNavPaneOpenState)
		{
			var settings = ApplicationData.Current.LocalSettings;

			if (settings.Values.TryGetValue($"{LocalSettingsKeyPrefix}_{SettingsName}", out var isPaneOpen))
			{
				navigationView.IsPaneOpen = (bool)isPaneOpen;
			}
			else
			{
				navigationView.IsPaneOpen = defaultNavPaneOpenState;
			}
		}
	}
}
