using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using Windows.Storage;
using WinRT.Interop;

namespace Aer.Utils
{
	class NavigationViewStateManager
	{
		private static string SettingsPrefix => nameof(NavigationViewStateManager);
		private const string SettingsName = "NavigationView_IsPaneOpen";

		public static void Save(NavigationView navigationView)
		{
			var settings = ApplicationData.Current.LocalSettings;

			settings.Values[$"{SettingsPrefix}_{SettingsName}"] = navigationView.IsPaneOpen;
		}

		public static void Restore(NavigationView navigationView, bool defaultNavPaneOpenState)
		{
			var settings = ApplicationData.Current.LocalSettings;

			if (settings.Values.TryGetValue($"{SettingsPrefix}_{SettingsName}", out var isPaneOpen))
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
