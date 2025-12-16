using Aer.Utils;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

namespace Aer
{
	public sealed partial class SettingsPage : Page
	{
		private Dictionary<string, GeoNames.GeoNamesLocation> _locationSuggestionsMap = new();

		public string AppName => Package.Current.DisplayName;
		public string Copyright => $"© {DateTime.Now.Year} {Package.Current.PublisherDisplayName}. All rights reserved.";
		public string AppVersion => $"Version {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";

		public bool IsThickLineEnabled
		{
			get => Preferences.UseThickChartLine;
			set => Preferences.SetLineThickness(value);
		}

		public SettingsPage()
		{
			InitializeComponent();
			
			Loaded += SettingsPage_Loaded;
		}

		private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateLocationSectionFromData();
			UpdateDataUIControls();
			UpdatePreferenceUIControls();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			if (e.Parameter is MainWindow.SettingsNavigationArgs args && args.FocusLocationSearch)
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
		}

		// Generic handler for all HyperlinkButton clicks
		private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is HyperlinkButton button)
			{
				if (button.DataContext is string url && !string.IsNullOrEmpty(url))
				{
					// Open the URL in default browser
					var uri = new Uri(url);
					await Launcher.LaunchUriAsync(uri);
				}
			}
		}
		
		// Generic handler for all hyperlink SettingsCard clicks
		private async void HyperlinkSettingsCard_Click(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement card &&
				card.DataContext is string url &&
				Uri.TryCreate(url, UriKind.Absolute, out var uri))
			{
				await Launcher.LaunchUriAsync(uri);
			}
		}
	
		#region Location
		private void UpdateLocationSectionFromData(bool acknowlidgeAChange = false)
		{
			LocationSettingsCard.Header = Data.LocationLabel!;
			LocationSettingsCard.Description = Data.LocationCoordinates!;
			
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
					Data.SetLocation(location.City, location.Country, location.Latitude, location.Longitude);
					
					UpdateLocationSectionFromData(true);
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
				await GeoNames.Load(); // Wait for it to finish
				LocationLoadingProgressRing.IsActive = false; // disable the ring
				
				// Data ready - continue with creating options
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
			_locationSuggestionsMap = new Dictionary<string, GeoNames.GeoNamesLocation>();
			foreach (var c in filteredGeoNames)
			{
				// Keep Admin1Code only if it present and not a digit
				string adminCode = string.IsNullOrWhiteSpace(c.Admin1Code) || double.TryParse(c.Admin1Code, out _) ? "" : $", {c.Admin1Code}";
				string key = $"{c.Name}, {c.Country}{adminCode}";

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
				Debug.WriteLine($"Chosen: {location.Name}, {location.Country} ({location.Latitude}, {location.Longitude})");

				Data.SetLocation(location.Name, location.Country, location.Latitude, location.Longitude);
				UpdateLocationSectionFromData(true);

				// Data will update when showing the HomePage
			}
		}
		#endregion

		#region Data
		private void WeatherProviderSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (((ComboBox)sender).SelectedItem is ComboBoxItem selectedItem)
			{
				int providerId = (int)selectedItem.Tag;
				Preferences.SetWeatherProviderId(providerId);
			}
		}

		private void UpdateDataUIControls()
		{
			// Clear any existing items
			WeatherProviderSelector.Items.Clear();

			// Populate Segmented control
			var providers = Weather.WeatherProvider.GetAllProvidersForUserSelection();
			foreach (var keyValuePair in providers)
			{
				var comboItem = new ComboBoxItem
				{
					Content = keyValuePair.Value, // Display name
					Tag = keyValuePair.Key // Provider Id
				};
				WeatherProviderSelector.Items.Add(comboItem);
			}

			var selectedItem = WeatherProviderSelector.Items
				.OfType<ComboBoxItem>()
				.FirstOrDefault(i => (int)i.Tag == Preferences.WeatherProviderId);

			if (selectedItem != null)
				WeatherProviderSelector.SelectedItem = selectedItem;
		}
		#endregion

		#region Preferences
		private void TempUnitSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Preferences.SetTemperatureUnits(
				(SegmentedItem)TempUnitSelector.SelectedItem == TempUnitCelsius
				? TemperatureUtils.Unit.Celsius
				: TemperatureUtils.Unit.Fahrenheit);
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

		private void AccentColorSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (((ComboBox)sender).SelectedItem is ComboBoxItem selectedItem)
			{
				if (bool.TryParse((string)selectedItem.Tag, out bool useSystemAccentColor))
				{
					Preferences.SetAccentColor(useSystemAccentColor);
					
					if (useSystemAccentColor != App.StartedUsingSystemAccentColor)
					{
						// Differs from the setting App started with
						RestartAppButton.Visibility = Visibility.Visible;
						CompositorAnimations.AnimatePop(RestartAppButton, 0.5);
					}
					else
					{
						RestartAppButton.Visibility = Visibility.Collapsed;
					}
				}
			}
		}

		private void RestartAppButton_Click(object sender, RoutedEventArgs e)
		{
			_ = App.Restart();
		}

		private void UpdatePreferenceUIControls()
		{
			TempUnitSelector.SelectedItem = Preferences.TemperatureUnits switch
			{
				TemperatureUtils.Unit.Celsius => TempUnitCelsius,
				TemperatureUtils.Unit.Fahrenheit => TempUnitFahrenheit,
				_ => null
			};

			AppThemeSelector.SelectedItem = Preferences.AppTheme switch
			{
				ElementTheme.Light => AppThemeLight,
				ElementTheme.Dark => AppThemeDark,
				ElementTheme.Default => AppThemeDefault,
				_ => null
			};

			AccentColorSelector.SelectedItem = Preferences.UseSystemAccentColor switch
			{
				true => AccentColorSystem,
				false => AccentColorAer
			};

			this.Bindings.Update(); // All UI refreshed
		}
		#endregion

		#region About and Debug
		public string? WindowSizeInfoText()
		{
			if (App.MainWindow == null) return null;

			var appWindow = WindowUtils.GetAppWindow(App.MainWindow);
			if (appWindow == null) return null;

			return "Window size: " + appWindow.Size.Width + "×" + appWindow.Size.Height;
		}

		private void OpenAppLocalFolderButton_Click(object sender, RoutedEventArgs e)
		{
			AppStorage.OpenLocalFolder();
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

			// 3. Delete app storage file
			AppStorage.Delete();

			// 4. Reload default values into data and preferences
			Data.LoadCacheOrDefaults();
			Preferences.Load();

			// 5. Refresh UI with default values
			UpdateLocationSectionFromData(true);
			UpdateDataUIControls();
			UpdatePreferenceUIControls();
		}
		#endregion

		private void ShareButton_Click(object sender, RoutedEventArgs e)
		{
			ShareHelper.ShowShare(
				App.MainWindow,
				AppName,
				(string)Application.Current.Resources["AppDescription"],
				new Uri((string)Application.Current.Resources["WebStoreURL"]));
		}
	}
}
