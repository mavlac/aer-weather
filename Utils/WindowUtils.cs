using Aer.Data;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using Windows.Graphics;
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

		internal static void SetAppIcon(Window window)
		{
			var appWindow = GetAppWindow(window);
			if (appWindow == null) return;
			
			// The.ico file should include multiple resolutions(16, 32, 48, 64, 128, 256).
			// Set its Build Action to Content and Copy to Output Directory to Do not copy.
			// Shows in: Title bar, Taskbar(runtime), Alt + Tab switcher
			// Does not override Start Menu icons - those still come from the manifest PNGs.
			appWindow.SetIcon("Assets/AppIcon.ico");
		}

		public static void InitializeTitleBar(Window window)
		{
			var appWindow = GetAppWindow(window);
			if (appWindow == null) return;

			appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
		}

		public static void UpdateTitleBarDraggableArea(Window window)
		{
			try
			{
				var appWindow = GetAppWindow(window);
				if (appWindow == null) return;

				const int LeftOffsetLogical = 48; // logical units like in XAML
				const int DragHeightLogical = 48;

				// Use XamlRoot.RasterizationScale for DPI scaling
				if (window?.Content is not FrameworkElement root || root.XamlRoot is null)
				{
					Debug.WriteLine("UpdateTitleBarDraggableArea: Unable to set: no current window or XamlRoot");
					return;
				}
				double scale = root.XamlRoot.RasterizationScale;

				int leftOffset = (int)(LeftOffsetLogical * scale);
				int dragHeight = (int)(DragHeightLogical * scale);

				int windowWidth = appWindow.Size.Width; // raw pixels

				if (windowWidth <= leftOffset) return;

				var dragRect = new RectInt32
				{
					X = leftOffset,
					Y = 0,
					Width = windowWidth - leftOffset,
					Height = dragHeight
				};

				appWindow.TitleBar.SetDragRectangles(new[] { dragRect });
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"UpdateTitleBarDraggableArea: Exception — {ex.Message}");
			}
		}

		internal static void ApplyAppTheme(Window window)
		{
			if (window.Content is FrameworkElement rootElement)
			{
				rootElement.RequestedTheme = Preferences.AppTheme;
				ApplyTitleBarTheme(window);
				ApplyAccentColor(); // This needs to reassign colors based on theme
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

		/// <summary>
		/// Set only on Application.OnLaunched - runtime changes are not applied
		/// </summary>
		internal static void ApplyAccentColor()
		{
			var resources = Application.Current.Resources;

			if (Preferences.UseSystemAccentColor)
			{
				// Use system accent color - nothing to override
				return;
			}

			// Use custom app accent

			bool isDarkTheme =
				Preferences.AppTheme == ElementTheme.Dark
				|| Preferences.AppTheme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark;

			var lightGray = (Color)resources["AerAccentColorLightGray"];
			var mediumGray = (Color)resources["AerAccentColorMediumGray"];
			var darkGray = (Color)resources["AerAccentColorDarkGray"];

			var primaryAccentColor = isDarkTheme ? lightGray : darkGray;
			var primaryAccentBrush = new SolidColorBrush(primaryAccentColor);
			var secondaryAccentBrush = new SolidColorBrush(mediumGray);

			// System accent colors - (respected in dark mode)
			resources["SystemAccentColor"] = primaryAccentColor;
			resources["SystemAccentColorLight1"] = lightGray;
			resources["SystemAccentColorLight2"] = lightGray;
			resources["SystemAccentColorLight3"] = mediumGray;
			resources["SystemAccentColorDark1"] = darkGray;
			resources["SystemAccentColorDark2"] = darkGray;
			resources["SystemAccentColorDark3"] = mediumGray;

			// Legacy Accent Brushes (some controls still use them)
			resources["SystemControlHighlightAccentBrush"] = primaryAccentBrush;
			resources["SystemControlForegroundAccentBrush"] = primaryAccentBrush;

			// Modern Accent Brushes (light theme especially needs these)
			resources["AccentFillColorDefaultBrush"] = primaryAccentBrush;
			resources["AccentFillColorSecondaryBrush"] = primaryAccentBrush;
			resources["AccentFillColorTertiaryBrush"] = secondaryAccentBrush;
			resources["AccentTextFillColorPrimaryBrush"] = primaryAccentBrush;
			resources["AccentTextFillColorSecondaryBrush"] = primaryAccentBrush;
			resources["AccentTextFillColorTertiaryBrush"] = secondaryAccentBrush;
			resources["AccentButtonBackgroundBrush"] = primaryAccentBrush;
			resources["AccentButtonBackgroundPressedBrush"] = secondaryAccentBrush;
			resources["AccentButtonBackgroundDisabledBrush"] = secondaryAccentBrush;
		}
	}
}
