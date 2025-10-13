using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Storage;
using WinRT.Interop;

namespace Aer.Utils
{
	public static class WindowPlacementManager
	{
		[DllImport("User32.dll")]
		private static extern int GetDpiForWindow(IntPtr hwnd);
		
		public static string SettingsPrefix => nameof(WindowPlacementManager);

		public static void Save(Window window)
		{
			var appWindow = WindowUtils.GetAppWindow(window);
			if (appWindow == null) return;

			var hwnd = WindowNative.GetWindowHandle(window);
			var dpi = GetDpiForWindow(hwnd);
			double scale = dpi / 96.0;

			var settings = ApplicationData.Current.LocalSettings;

			var pos = appWindow.Position;
			var size = appWindow.Size;

			settings.Values[$"{SettingsPrefix}_X"] = (int)(pos.X / scale);
			settings.Values[$"{SettingsPrefix}_Y"] = (int)(pos.Y / scale);
			settings.Values[$"{SettingsPrefix}_W"] = (int)(size.Width / scale);
			settings.Values[$"{SettingsPrefix}_H"] = (int)(size.Height / scale);

			// SaveWeatherData maximized state (ignore minimized)
			settings.Values[$"{SettingsPrefix}_IsMaximized"] =
				appWindow.Presenter is OverlappedPresenter o &&
				o.State == OverlappedPresenterState.Maximized;
		}

		public static void Restore(Window window, int defaultWidth, int defaultHeight)
		{
			var appWindow = WindowUtils.GetAppWindow(window);
			if (appWindow == null) return;

			var hwnd = WindowNative.GetWindowHandle(window);
			var dpi = GetDpiForWindow(hwnd);
			double scale = dpi / 96.0;

			var settings = ApplicationData.Current.LocalSettings;

			// try size + position
			if (settings.Values.TryGetValue($"{SettingsPrefix}_W", out var wObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_H", out var hObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_X", out var xObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_Y", out var yObj))
			{
				int x = (int)((int)xObj * scale);
				int y = (int)((int)yObj * scale);
				int width = (int)((int)wObj * scale);
				int height = (int)((int)hObj * scale);

				var rect = new RectInt32(x, y, width, height);

				if (IsRectVisible(rect, window))
				{
					appWindow.MoveAndResize(rect);
				}
				else
				{
					appWindow.Resize(new SizeInt32((int)(defaultWidth * scale), (int)(defaultHeight * scale)));
				}
			}
			else
			{
				appWindow.Resize(new SizeInt32(defaultWidth, defaultHeight));
			}

			// try maximized
			if (settings.Values.TryGetValue($"{SettingsPrefix}_IsMaximized", out var maxObj) &&
				maxObj is bool isMaximized &&
				isMaximized &&
				appWindow.Presenter is OverlappedPresenter overlapped)
			{
				overlapped.Maximize();
			}
		}

		private static bool IsRectVisible(RectInt32 rect, Window window)
		{
			var hwnd = WindowNative.GetWindowHandle(window);
			var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);

			// Get the display that contains (or would contain) the window
			var display = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
			var bounds = display.OuterBounds;

			// check overlap
			return rect.X < bounds.X + bounds.Width &&
				   rect.X + rect.Width > bounds.X &&
				   rect.Y < bounds.Y + bounds.Height &&
				   rect.Y + rect.Height > bounds.Y;
		}
	}
}
