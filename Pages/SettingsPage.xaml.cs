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
		private Dictionary<string, GeoNames.GeoNamesLocation> locationSuggestionsMap = new();

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
			LocationSettingsCard.Header = Data.LocationLabel ?? "Unknown location";
			LocationSettingsCard.Description = Data.LocationCoordinates ?? "No coordinates";
		}

		private void LocationAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
				return;

			if (!GeoNames.IsLoaded)
			{
				if (!GeoNames.IsLoading)
					Task.Run(GeoNames.Load);
				
				return;
			}

			string query = sender.Text;

			// Clear suggestions if query is too short
			if (query.Length < 2)
			{
				sender.ItemsSource = null;
				return;
			}

			// Build suggestions

			// Find locations where name or any alternate name starts with the query (case insensitive)
			var filteredGeoNames = GeoNames.AllGeoNamesLocations
				.Where(c =>
					c.NameASCII.StartsWith(query, StringComparison.InvariantCultureIgnoreCase)
					|| (c.AlternateNames?.Split(',').Any(a => a.Trim().StartsWith(query, StringComparison.InvariantCultureIgnoreCase)) ?? false))
				.OrderByDescending(c => c.Population)
				.Take(15)
				.ToList();

			// Dictionary of labels and location objects for easy lookup when suggestion is chosen
			locationSuggestionsMap = filteredGeoNames.ToDictionary(
				c =>
				{
					string adminCode = string.IsNullOrWhiteSpace(c.Admin1Code) || double.TryParse(c.Admin1Code, out _)
						? ""
						: $", {c.Admin1Code}";
					return $"{c.Name}, {c.Country}{adminCode}";
				}
			);

			sender.ItemsSource = locationSuggestionsMap.Keys.ToList();
		}

		private void LocationAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			if (args.SelectedItem is string text && locationSuggestionsMap.TryGetValue(text, out var location))
			{
				Debug.WriteLine($"Chosen: {location.Name}, {location.Country} ({location.Latitude}, {location.Longitude})");

				Data.SetLocation(location.Name, location.Country, location.Latitude, location.Longitude);
				UpdateLocationSectionFromData();
				sender.SetValue(AutoSuggestBox.TextProperty, ""); // Clear text
				// TODO: Highlighting effect

				// TODO
				//Data.UpdateWeatherDataFromNetwork();
			}
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
