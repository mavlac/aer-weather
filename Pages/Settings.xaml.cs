using Aer.Data;
using Aer.Utils;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;

namespace Aer.Pages
{

	public sealed partial class Settings : Page
	{
		private bool _isUpdatingWeatherProviderSelector;

		public string AppName => Package.Current.DisplayName;
		public string Copyright => $"\u00A9 {DateTime.Now.Year} {Package.Current.PublisherDisplayName}. All rights reserved.";
		public string AppVersion => $"Version {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";

		public bool IsThickLineEnabled
		{
			get => Preferences.UseThickChartLine;
			set => Preferences.SetLineThickness(value);
		}

		public Settings()
		{
			InitializeComponent();
			
			Loaded += SettingsPage_Loaded;
		}

		private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateDataUIControls();
			UpdatePreferenceUIControls();
			UpdateAboutUIControls();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			MainWindow.GlobalHotkeyPressed += MainWindow_GlobalHotkeyPressed;
			MainWindow.WindowSizeChanged += MainWindow_WindowSizeChanged;
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);

			MainWindow.GlobalHotkeyPressed -= MainWindow_GlobalHotkeyPressed;
			MainWindow.WindowSizeChanged -= MainWindow_WindowSizeChanged;
		}

		private void MainWindow_GlobalHotkeyPressed(MainWindow.GlobalHotkey globalHotkey)
		{
			switch (globalHotkey)
			{
				case MainWindow.GlobalHotkey.BackToHomePage:
					App.MainWindow.NavigateToHomePage();
					break;

				case MainWindow.GlobalHotkey.OpenLocationPage:
					App.MainWindow.NavigateToLocationPage(false);
					break;

				case MainWindow.GlobalHotkey.DarkThemeToggle:
					Preferences.ToggleDarkAndLightTheme();
					UpdatePreferenceUIControls();
					this.Bindings.Update();
					break;
			}
		}

		private void MainWindow_WindowSizeChanged(Size size)
		{
			this.Bindings.Update(); // Debug window size info in About section
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

		#region Data
		private void UpdateDataUIControls()
		{
			_isUpdatingWeatherProviderSelector = true;
			
			try
			{
				// Clear and populate ComboBox items
				WeatherProviderSelector.Items.Clear();
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
			finally
			{
				_isUpdatingWeatherProviderSelector = false;
			}
		}

		private void WeatherProviderSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (((ComboBox)sender).SelectedItem is ComboBoxItem selectedItem)
			{
				int providerId = (int)selectedItem.Tag;
				Preferences.SetWeatherProviderId(providerId);

				// Experimental feature warning for Yr.No
				if (!_isUpdatingWeatherProviderSelector)
				{
					if (providerId == Weather.YrNo.YrNoWeatherProvider.ProviderStaticId)
					{
						WeatherProviderInfoBar.Title = "Experimental Feature";
						WeatherProviderInfoBar.Message = "The Yr weather provider powered by the MET Norway Locationforecast API is an experimental feature with limited functionality. It does not support apparent temperature, and hourly forecasts are limited to approximately three days. It may not behave as expected and may be removed in a future update.";
						WeatherProviderInfoBar.Visibility = Visibility.Visible;
						WeatherProviderInfoBar.IsOpen = true;
					}
					else
					{
						WeatherProviderInfoBar.IsOpen = false;
						WeatherProviderInfoBar.Visibility = Visibility.Collapsed;
					}
				}
			}
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
						CompositorAnimations.AnimatePop(RestartAppButton, 1.1f, 0.5d);
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
		}
		#endregion

		#region About and Debug
		private void UpdateAboutUIControls()
		{
			var stats = WeatherDataCache.GetStatistics();
			DebugInfoDatabaseStatsText.Text = $"Cached records: {stats.Total} ({stats.Expired} expired)";
		}

		public string? WindowSizeInfoText()
		{
			if (App.MainWindow == null) return null;

			var appWindow = WindowUtils.GetAppWindow(App.MainWindow);
			if (appWindow == null) return null;

			return "Window size: " + appWindow.Size.Width + "\u00D7" + appWindow.Size.Height;
		}

		private void OpenAppLocalFolderButton_Click(object sender, RoutedEventArgs e)
		{
			AppStorage.OpenLocalFolder();
		}

		private void ClearLocalSettingsButton_Click(object sender, RoutedEventArgs e)
		{
			var localSettings = ApplicationData.Current.LocalSettings;

			// 1. Clear all local settings
			// Clear all root-level key/value pairs
			localSettings.Values.Clear();
			// Delete all sub-containers, if any
			var containersToRemove = localSettings.Containers.Keys.ToList(); // copy keys to avoid modifying collection while iterating
			foreach (var containerName in containersToRemove)
			{
				localSettings.DeleteContainer(containerName);
			}

			// 2. Delete app storage file
			AppStorage.Delete();

			// 3. Reset location and delete cache data
			LocationManager.ClearRecents();
			LocationManager.Load(); // Will reset to default
			WeatherDataCache.ResetCache();

			// 4. Reload default preferences
			Preferences.Load();

			// 5. Refresh UI with default values
			UpdateDataUIControls();
			UpdatePreferenceUIControls();
			UpdateAboutUIControls();
			this.Bindings.Update();
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
