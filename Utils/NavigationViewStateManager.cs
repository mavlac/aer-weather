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
		public static string SettingsPrefix => nameof(NavigationViewStateManager);

		public static void Save(NavigationView navigationView)
		{
			var settings = ApplicationData.Current.LocalSettings;

			settings.Values[$"{SettingsPrefix}_NavigationView_IsPaneOpen"] = navigationView.IsPaneOpen;
		}

		public static void Restore(NavigationView navigationView, bool defaultNavPaneOpenState)
		{
			var settings = ApplicationData.Current.LocalSettings;

			if (settings.Values.TryGetValue($"{SettingsPrefix}_NavigationView_IsPaneOpen", out var isPaneOpen))
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
