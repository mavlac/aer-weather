using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Aer.Utils
{
	public static class MessageBoxEx
	{
		public static async Task<bool> ShowAsync(string title, string message, string primaryButtonText, string? closeButtonText = null)
		{
			// Get current window’s root
			var window = App.MainWindow;
			if (window?.Content is not FrameworkElement root || root.XamlRoot is null)
			{
				Debug.WriteLine("MessageBoxEx: Unable to show: no current window or XamlRoot");
				return false;
			}

			var dialog = new ContentDialog
			{
				Title = title,
				Content = message,
				XamlRoot = root.XamlRoot,
				RequestedTheme = root.ActualTheme
			};

			// Configure buttons
			dialog.PrimaryButtonText = primaryButtonText;
			if (!string.IsNullOrEmpty(closeButtonText))
				dialog.CloseButtonText = closeButtonText;

			var result = await dialog.ShowAsync();

			return result == ContentDialogResult.Primary;
		}
	}
}
