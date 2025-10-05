using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using Windows.Storage;
using WinRT.Interop;

namespace Aer.Utils
{
	public static class WindowPlacementManager
	{
		public static string SettingsPrefix => nameof(WindowPlacementManager);

		public static void Save(Window window)
		{
			var appWindow = WindowUtils.GetAppWindow(window);
			if (appWindow == null) return;

			var settings = ApplicationData.Current.LocalSettings;

			var pos = appWindow.Position;
			var size = appWindow.Size;

			settings.Values[$"{SettingsPrefix}_X"] = pos.X;
			settings.Values[$"{SettingsPrefix}_Y"] = pos.Y;
			settings.Values[$"{SettingsPrefix}_W"] = size.Width;
			settings.Values[$"{SettingsPrefix}_H"] = size.Height;

			// SaveWeatherData maximized state (ignore minimized)
			settings.Values[$"{SettingsPrefix}_IsMaximized"] =
				appWindow.Presenter is OverlappedPresenter o &&
				o.State == OverlappedPresenterState.Maximized;
		}

		public static void Restore(Window window, int defaultWidth, int defaultHeight)
		{
			var appWindow = WindowUtils.GetAppWindow(window);
			if (appWindow == null) return;

			var settings = ApplicationData.Current.LocalSettings;

			// try size + position
			if (settings.Values.TryGetValue($"{SettingsPrefix}_W", out var wObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_H", out var hObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_X", out var xObj) &&
				settings.Values.TryGetValue($"{SettingsPrefix}_Y", out var yObj))
			{
				int x = (int)xObj;
				int y = (int)yObj;
				int width = (int)wObj;
				int height = (int)hObj;

				var rect = new RectInt32(x, y, width, height);

				if (IsRectVisible(rect, window))
				{
					appWindow.MoveAndResize(rect);
				}
				else
				{
					appWindow.Resize(new SizeInt32(defaultWidth, defaultHeight));
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
