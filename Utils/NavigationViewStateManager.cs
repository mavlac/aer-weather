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
		public static void Save(NavigationView navigationView)
		{
			var settings = ApplicationData.Current.LocalSettings;
			var prefix = navigationView.GetType().Name;

			settings.Values[$"{prefix}_NavigationView_IsPaneOpen"] = navigationView.IsPaneOpen;
		}

		public static void Restore(NavigationView navigationView, bool defaultNavPaneOpenState)
		{
			var settings = ApplicationData.Current.LocalSettings;
			var prefix = navigationView.GetType().Name;

			if (settings.Values.TryGetValue($"{prefix}_NavigationView_IsPaneOpen", out var isPaneOpen))
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
