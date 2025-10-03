using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
	}
}
