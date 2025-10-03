using Aer.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Aer
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class SettingsPage : Page
	{
		public string AppVersion => $"Version {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";

		public SettingsPage()
		{
			InitializeComponent();
			
			UpdateLocationSectionFromData();
		}

		// Generic handler for all HyperlinkButton clicks
		private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is HyperlinkButton button && button.Content is TextBlock textBlock)
			{
				if (textBlock.DataContext is string url && !string.IsNullOrEmpty(url))
				{
					// Open the URL in default browser
					var uri = new Uri(url);
					await Windows.System.Launcher.LaunchUriAsync(uri);
				}
			}
		}


		#region Location Section
		private void UpdateLocationSectionFromData()
		{
			// Fallbacks should never be needed, default location and coordinates are used if nothing else is set
			LocationSettingsCard.Header = Data.LocationName ?? "Unknown location";
			LocationSettingsCard.Description = Data.LocationCoordinates ?? "No coordinates";
		}

		private void LocationAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
				return;

			if (!GeoNames.IsLoaded)
			{
				Task.Run(GeoNames.Load);
			}

			string query = sender.Text;

			// Clear suggestions if query is too short
			if (query.Length < 2)
			{
				sender.ItemsSource = null;
				return;
			}

			// Simple filter
			var results = GeoNames.AllGeoNamesLocations
				.Where(c => c.City.StartsWith(query, StringComparison.InvariantCultureIgnoreCase))
				.Take(10)
				.Select(c => $"{c.City}, {c.Country}")
				.ToList();

			sender.ItemsSource = results;
		}

		private void LocationAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			Debug.WriteLine("Suggestion chosen: " + args.SelectedItem);

			// TODO
			//Data.SetLocation(...);
			//Data.UpdateFromNetworkDataProvider();
		}
		#endregion

		#region About Section
		public string? WindowSizeInfoText()
		{
			if (App.MainWindow == null) return null;

			var appWindow = WindowUtils.GetAppWindow(App.MainWindow);
			if (appWindow == null) return null;

			return "Window size: " + appWindow.Size.Width + ", " + appWindow.Size.Height;
		}

		private void ClearLocalSettingsButton_Click(object sender, RoutedEventArgs e)
		{
			Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
			var containersToRemove = new List<string>(localSettings.Containers.Keys); // Create a list of keys to avoid modifying while iterating

			foreach (var key in containersToRemove)
			{
				localSettings.DeleteContainer(key);
			}
		}
		#endregion
	}
}
