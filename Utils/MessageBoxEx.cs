using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Aer.Utils
{
	public static class MessageBoxEx
	{
		public static async Task ShowAsync(string title, string message)
		{
			// Get current window’s root
			var window = App.MainWindow;
			if (window?.Content is not FrameworkElement root || root.XamlRoot is null)
			{
				Debug.WriteLine("MessageBoxEx: Unable to show: no current window or XamlRoot");
				return;
			}

			var dialog = new ContentDialog
			{
				Title = title,
				Content = message,
				CloseButtonText = "OK",
				XamlRoot = root.XamlRoot,
				RequestedTheme = root.ActualTheme // matches light/dark mode
			};

			await dialog.ShowAsync();
		}
	}
}
