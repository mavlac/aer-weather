using Aer.Utils;
using CommunityToolkit.WinUI.Controls;
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
			
			Loaded += SettingsPage_Loaded;
		}

		private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateLocationSectionFromData();
			UpdatePreferences();
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

		#region Location
		private void UpdateLocationSectionFromData(bool acknowlidgeAChange = false)
		{
			// Fallbacks should never be needed, default location and coordinates are used if nothing else is set
			LocationSettingsCard.Header = Data.LocationLabel ?? "Unknown location";
			LocationSettingsCard.Description = Data.LocationCoordinates ?? "No coordinates";

			if (acknowlidgeAChange)
			{
				// Highlight changes in LocationSettingsCard
				var iconPresenter = FrameworkUtils.FindChildByName<FrameworkElement>(LocationSettingsCard, "PART_HeaderIconPresenter");
				if (iconPresenter != null) CompositorAnimations.AnimatePop(iconPresenter, 0.5);
				var headerPresenter = FrameworkUtils.FindChildByName<FrameworkElement>(LocationSettingsCard, "PART_HeaderPresenter");
				if (headerPresenter != null) CompositorAnimations.AnimateFadeIn(headerPresenter, 0.5);
				var descriptionPresenter = FrameworkUtils.FindChildByName<FrameworkElement>(LocationSettingsCard, "PART_DescriptionPresenter");
				if (descriptionPresenter != null) CompositorAnimations.AnimateFadeIn(descriptionPresenter, 0.5);
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
			locationSuggestionsMap = new Dictionary<string, GeoNames.GeoNamesLocation>();
			foreach (var c in filteredGeoNames)
			{
				string adminCode = string.IsNullOrWhiteSpace(c.Admin1Code) || double.TryParse(c.Admin1Code, out _)
					? ""
					: $", {c.Admin1Code}";
				string key = $"{c.Name}, {c.Country}{adminCode}";

				// Only add if not present (or replace if population is higher)
				if (!locationSuggestionsMap.TryGetValue(key, out var existing) || c.Population > existing.Population)
					locationSuggestionsMap[key] = c;
			}

			sender.ItemsSource = locationSuggestionsMap.Keys.ToList();
		}

		private void LocationAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			if (args.SelectedItem is string text && locationSuggestionsMap.TryGetValue(text, out var location))
			{
				Debug.WriteLine($"Chosen: {location.Name}, {location.Country} ({location.Latitude}, {location.Longitude})");

				Data.SetLocation(location.Name, location.Country, location.Latitude, location.Longitude);
				UpdateLocationSectionFromData(true);
				//sender.SetValue(AutoSuggestBox.TextProperty, "");

				// TODO
				//Data.UpdateWeatherDataFromNetwork();
			}
		}
		#endregion

		#region Preferences Section
		private void TempUnitSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Preferences.SetTemperatureUnits(
				(SegmentedItem)TempUnitSelector.SelectedItem == TempUnitCelsius
				? Preferences.TemperatureUnit.Celsius
				: Preferences.TemperatureUnit.Fahrenheit);
		}

		private void AppThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (((ComboBox)sender).SelectedItem is ComboBoxItem selectedItem)
			{
				if (Enum.TryParse((string)selectedItem.Tag, out ElementTheme selectedTheme))
				{
					Preferences.SetAppTheme(selectedTheme);
					WindowUtils.ApplyAppTheme(App.MainWindow);
				}
			}
		}

		private void UpdatePreferences()
		{
			TempUnitSelector.SelectedItem = Preferences.TemperatureUnits switch
			{
				Preferences.TemperatureUnit.Celsius => TempUnitCelsius,
				Preferences.TemperatureUnit.Fahrenheit => TempUnitFahrenheit,
				_ => null
			};
			AppThemeSelector.SelectedItem = Preferences.AppTheme switch
			{
				ElementTheme.Light => AppThemeLight,
				ElementTheme.Dark => AppThemeDark,
				ElementTheme.Default => AppThemeDefault,
				_ => null
			};
		}
		#endregion

		#region About and Debug Section
		public string? WindowSizeInfoText()
		{
			if (App.MainWindow == null) return null;

			var appWindow = WindowUtils.GetAppWindow(App.MainWindow);
			if (appWindow == null) return null;

			return "Window size: " + appWindow.Size.Width + "×" + appWindow.Size.Height;
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

			// 3. Reload default values into data model and preferences
			Data.LoadLastSavedValues();
			Preferences.Load();

			// 4. Refresh UI with default values
			UpdateLocationSectionFromData(true);
			UpdatePreferences();
		}
		#endregion
    }
}
