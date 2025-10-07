using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using Windows.UI;
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
			var appWindow = GetAppWindow(window);
			if (appWindow == null) return;

			appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
		}

		internal static void ApplyAppTheme(Window window)
		{
			if (window.Content is FrameworkElement rootElement)
			{
				rootElement.RequestedTheme = Preferences.AppTheme;
				ApplyTitleBarTheme(window);
			}
		}

		private static void ApplyTitleBarTheme(Window window)
		{
			var appWindow = GetAppWindow(window);
			if (appWindow == null) return;

			bool isDarkTheme = GetIsDarkTheme(window);

			// Transparent backgrounds so your custom title bar content shows
			appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

			// Set the foregrounds according to theme
			appWindow.TitleBar.ButtonForegroundColor = isDarkTheme ? Colors.White : Colors.Black;
			appWindow.TitleBar.ButtonHoverForegroundColor = isDarkTheme ? Colors.White : Colors.Black;
			appWindow.TitleBar.ButtonHoverBackgroundColor = isDarkTheme ? Color.FromArgb(20, 255, 255, 255)
																		: Color.FromArgb(20, 0, 0, 0);
			appWindow.TitleBar.ButtonPressedBackgroundColor = isDarkTheme ? Color.FromArgb(60, 255, 255, 255)
																		  : Color.FromArgb(40, 0, 0, 0);
			appWindow.TitleBar.ButtonInactiveForegroundColor = isDarkTheme ? Color.FromArgb(255, 170, 170, 170)
																		   : Color.FromArgb(255, 100, 100, 100);

			static bool GetIsDarkTheme(Window window)
			{
				if (window.Content is FrameworkElement frameworkElement)
				{
					return frameworkElement.ActualTheme == ElementTheme.Dark;
				}
				return false;
			}
		}
	}
}
