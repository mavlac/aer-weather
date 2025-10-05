using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Aer.Utils
{
	internal class WindowUtils
	{
		public static AppWindow? GetAppWindow(Window window)
		{
			var hwnd = WindowNative.GetWindowHandle(window);
			var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
			return AppWindow.GetFromWindowId(windowId);
		}

		public static void InitializeTitleBar(Window window)
		{
			// Get the AppWindow
			var appWindow = GetAppWindow(window);
			if (appWindow == null) return;

			appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			// Set transparent button backgrounds
			appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
		}
	}
}
