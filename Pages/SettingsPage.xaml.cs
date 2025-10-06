using Aer.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace Aer
{
	public sealed partial class SettingsPage : Page
	{
		private Dictionary<string, GeoNames.GeoNamesLocation> locationSuggestionsMap = new();

		public string AppName => Package.Current.DisplayName;
		public string Copyright => $"© {DateTime.Now.Year} {Package.Current.PublisherDisplayName}. All rights reserved.";
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
		private void UpdateLocationSectionFromData(bool acknowlidgeAChange = false)
		{
			// Fallbacks should never be needed, default location and coordinates are used if nothing else is set
			LocationSettingsCard.Header = Data.LocationLabel ?? "Unknown location";
			LocationSettingsCard.Description = Data.LocationCoordinates ?? "No coordinates";

			if (acknowlidgeAChange)
				CompositorAnimations.AnimatePop(FindFirstChildTextBlock(LocationSettingsCard));
			// Will pop the Icon, because it is a TextBlock as well, but that's acceptable
			TextBlock FindFirstChildTextBlock(DependencyObject parent)
			{
				int count = VisualTreeHelper.GetChildrenCount(parent);
				for (int i = 0; i < count; i++)
				{
					var child = VisualTreeHelper.GetChild(parent, i);
					if (child is TextBlock tb)
						return tb;

					var result = FindFirstChildTextBlock(child);
					if (result != null)
						return result;
				}
				return null;
			}
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
				UpdateLocationSectionFromData(true);
				sender.SetValue(AutoSuggestBox.TextProperty, ""); // Clear text

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

			return "Window size: " + appWindow.Size.Width + " × " + appWindow.Size.Height;
		}

		private void ClearLocalSettingsButton_Click(object sender, RoutedEventArgs e)
		{
			var settings = ApplicationData.Current.LocalSettings;

			// 1. Clear all root-level key/value pairs
			settings.Values.Clear();

			// 2. Delete all sub-containers, if any
			var containersToRemove = settings.Containers.Keys.ToList(); // copy keys to avoid modifying collection while iterating
			foreach (var containerName in containersToRemove)
			{
				settings.DeleteContainer(containerName);
			}

			// 3. Reload default values into your data model
			Data.LoadLastSavedValues();

			// 4. Refresh UI with default values
			UpdateLocationSectionFromData(true);
		}
		#endregion
	}
}
