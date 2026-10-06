using Aer.Data;
using Aer.Utils;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Aer.Pages
{
	public sealed partial class Location : Page
	{
		private Dictionary<string, GeoNames.GeoNamesLocation> _locationSuggestionsMap = [];

		private static bool IsHistoryAvailable => LocationManager.RecentLocations.Count > 1;

		public Location()
		{
			InitializeComponent();

			Loaded += LocationPage_Loaded;
		}

		private void LocationPage_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateLocationSection(false);
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			if (e.Parameter is MainWindow.LocationNavigationArgs args && args.FocusSearchInput)
			{
				// Onboarding
				LocationAutoSuggestBoxTeachingTip.IsOpen = true;

				Loaded += (_, __) =>
				{
					DispatcherQueue.TryEnqueue(() =>
					{
						// Focus but wait until loaded and after the current UI pass completes
						LocationAutoSuggestBox.Focus(FocusState.Programmatic);
					});
				};
			}

			MainWindow.GlobalHotkeyPressed += MainWindow_GlobalHotkeyPressed;
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);

			MainWindow.GlobalHotkeyPressed -= MainWindow_GlobalHotkeyPressed;
		}

		private void MainWindow_GlobalHotkeyPressed(MainWindow.GlobalHotkey globalHotkey)
		{
			switch (globalHotkey)
			{
				case MainWindow.GlobalHotkey.BackToHomePage:
					App.MainWindow.NavigateToHomePage();
					break;

				case MainWindow.GlobalHotkey.OpenSettingsPage:
					App.MainWindow.NavigateToSettingsPage();
					break;

				case MainWindow.GlobalHotkey.DarkThemeToggle:
					Preferences.ToggleDarkAndLightTheme();
					break;
			}
		}

		private void UpdateLocationSection(bool popIfChanged)
		{
			bool didChange =
				(string)LocationSettingsCard.Header != LocationManager.CurrentLocation?.Label ||
				(string)LocationSettingsCard.Description != LocationManager.CurrentLocation?.ReadableCoordinates;

			LocationSettingsCard.Header = LocationManager.CurrentLocation?.Label!;
			LocationSettingsCard.Description = LocationManager.CurrentLocation?.ReadableCoordinates!;

			// Highlight changes in LocationSettingsCard
			if (popIfChanged)
			{
				// Icon always
				var iconPresenter = FrameworkUtils.FindChildByName<FrameworkElement>(LocationSettingsCard, "PART_HeaderIconPresenter");
				CompositorAnimations.AnimatePop(iconPresenter!, 1.2f, 0.5d);
				// Text only if did change
				if (didChange)
				{
					var headerPresenter = FrameworkUtils.FindChildByName<FrameworkElement>(LocationSettingsCard, "PART_HeaderPresenter");
					CompositorAnimations.AnimateFadeIn(headerPresenter!, 1d);
					var descriptionPresenter = FrameworkUtils.FindChildByName<FrameworkElement>(LocationSettingsCard, "PART_DescriptionPresenter");
					CompositorAnimations.AnimateFadeIn(descriptionPresenter!, 1d);
				}
			}

			PopulateRecentLocationButtons();
		}

		private async void UseCurrentLocationButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button)
			{
				button.IsEnabled = false;

				var location = await IpInfoHelper.GetLocationAsync();
				if (location != null
					&& !string.IsNullOrWhiteSpace(location.City)
					&& !string.IsNullOrWhiteSpace(location.Country))
				{
					LocationManager.Set(location.City, location.Country, location.Latitude, location.Longitude);

					UpdateLocationSection(true);
				}

				button.IsEnabled = true;
			}
		}

		private async void LocationAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
				return;

			LocationAutoSuggestBoxTeachingTip.IsOpen = false;

			if (!GeoNames.IsLoaded)
			{
				if (GeoNames.IsLoading)
					return;

				LocationLoadingProgressRing.IsActive = true;
				await GeoNames.Load(App.ShutdownToken); // Wait for it to finish and respect app shutdown
				LocationLoadingProgressRing.IsActive = false; // disable the ring

				// LocationAndCacheData ready - continue with creating options
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
			_locationSuggestionsMap = [];
			foreach (var c in filteredGeoNames)
			{
				// Keep Admin1Code only if it present and not a digit
				string adminCode = string.IsNullOrWhiteSpace(c.Admin1Code) || double.TryParse(c.Admin1Code, out _) ? "" : $", {c.Admin1Code}";
				string key = $"{c.Name}, {c.CountryCode}{adminCode}";

				// Only add if not present (or replace if population is higher), keys can repeat
				if (!_locationSuggestionsMap.TryGetValue(key, out var existing) || c.Population > existing.Population)
					_locationSuggestionsMap[key] = c;
			}

			sender.ItemsSource = _locationSuggestionsMap.Keys.ToList();
		}

		private void LocationAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			if (args.SelectedItem is string text && _locationSuggestionsMap.TryGetValue(text, out var location))
			{
				Debug.WriteLine($"Chosen: {location.Name}, {location.CountryCode} ({location.Latitude}, {location.Longitude})");

				LocationManager.Set(location.Name, location.CountryCode, location.Latitude, location.Longitude);
				UpdateLocationSection(true);

				// LocationAndCacheData will update when showing the Home
			}
		}

		private void PopulateRecentLocationButtons()
		{
			RecentlyUsedLocationsCard.Header =
				IsHistoryAvailable
					? "Recently used locations"
					: "No recently used locations";

			RecentLocationsStackPanel.Children.Clear();

			if (IsHistoryAvailable)
			{
				foreach (var location in LocationManager.RecentLocations)
				{
					// Skip first, first is the current location, we don't need a button for it
					if (location == LocationManager.CurrentLocation)
						continue;

					var button = new HyperlinkButton
					{
						Content = location.Label,
						DataContext = location
					};
					var panel = new StackPanel
					{
						Orientation = Orientation.Horizontal
					};
					panel.Children.Add(new FontIcon
					{
						Glyph = "\uE7B7", // MapPin2 Icon
						FontSize = 15,
						Margin = new Thickness(-3, 0, 8, 0)
					});
					panel.Children.Add(new TextBlock
					{
						Text = location.Label
					});

					button.Content = panel;
					button.Click += RecentLocationButton_Click;

					RecentLocationsStackPanel.Children.Add(button);
				}
			}
		}

		private void RecentLocationButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is HyperlinkButton recentLocationButton)
			{
				if (recentLocationButton.DataContext is LocationManager.Location selectedLocation)
				{
					LocationManager.Set(selectedLocation);
					
					App.MainWindow.NavigateToHomePage();
				}
			}
		}
	}
}
