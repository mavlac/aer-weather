using Aer.Data;
using Aer.Drawing;
using Aer.Utils;
using Aer.Utils.Extensions;
using Aer.Weather;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Aer
{
	public sealed partial class HomePage : Page
	{
		public const string NavigationTag = "home";

		private readonly MinuteTimer _minuteTimer = new();

		private Task<(bool status, string message)>? _updateTask;
		private CancellationTokenSource? _updateTaskCts;

		public HomePage()
		{
			InitializeComponent();

			// This should be immediate, but can contain default location with no weather data
			LocationAndCacheData.LoadCacheOrDefaults();

			Loading += HomePage_Loading;
			Loaded += HomePage_Loaded;

			_minuteTimer.Start(OnMinuteTick);
		}

		private async void HomePage_Loading(FrameworkElement sender, object args)
		{
			if (!Preferences.WasWelcomeShown)
			{
				Preferences.SetWelcomeShown(true);

				bool goToSettings = await MessageBoxEx.ShowAsync(
					$"Welcome to {Package.Current.DisplayName}!",
					"Thank you for using my weather app.\r\n\r\nThe default location is shown for now.\r\nSet your preferred location in Settings.",
					primaryButtonText: "Open Settings");

				if (goToSettings)
				{
					// Navigate to Settings page
					App.MainWindow.NavigateToSettingsPage(true);
				}
			}
		}

		private void HomePage_Loaded(object sender, RoutedEventArgs e)
		{
			// Show whatever is available immediately
			// Can be default location with no weather data, can be old cached data, can be valid current data
			UpdatePageContent();

			UpdateDataFromNetwork();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			MainWindow.GlobalHotkeyPressed += MainWindow_GlobalHotkeyPressed;
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			
			_minuteTimer.Stop();

			_updateTaskCts?.Cancel();
			_updateTaskCts?.Dispose();
			_updateTaskCts = null;

			MainWindow.GlobalHotkeyPressed -= MainWindow_GlobalHotkeyPressed;
		}

		private void MainWindow_GlobalHotkeyPressed(MainWindow.GlobalHotkey obj)
		{
			if (obj == MainWindow.GlobalHotkey.DarkThemeToggle)
			{
				ToggleDarkAndLightTheme();
			}
		}

		private void OnMinuteTick()
		{
			UpdatePageContent(); // Among other content will update the LastUpdateTimeText

			// LocationAndCacheData Update
			// If cache is still valid, no network call will be made.
			UpdateDataFromNetwork();
		}

		private async void UpdateDataFromNetwork()
		{
			if (_updateTask != null)
				return;

			// Cancel any previous operation first
			_updateTaskCts?.Cancel();
			_updateTaskCts?.Dispose();

			_updateTaskCts = new();
			var cancellationToken = _updateTaskCts.Token;

			// Kick off the network update, but don't await it yet
			_updateTask = LocationAndCacheData.UpdateWeatherDataFromNetwork(cancellationToken);

			// Wait briefly before deciding to show loader, cancellable
			try
			{
				await Task.Delay(200, cancellationToken);
			}
			catch (TaskCanceledException)
			{
				_updateTask = null;
				return;
			}

			// Only show loader and loading status if still not finished
			if (!_updateTask.IsCompleted)
			{
				LoadingOverlay.Visibility = Visibility.Visible;
				LastUpdateTimeText.Text = "Loading from network…";
			}

			// Await completion (still necessary to observe exceptions etc.)
			var (didUpdateSucceed, updateErrorMessage) = await _updateTask;

			// Whatever happened, does not matter, hide loader
			LoadingOverlay.Visibility = Visibility.Collapsed;
			UpdatePageContent(); // Will update the content and LastUpdateTimeText

			if (!didUpdateSucceed)
			{
				await MessageBoxEx.ShowAsync(
					"Unable to update weather data",
					$"Could not load new weather data from the network.\r\nPlease check your internet connection and restart the app.\r\n\r\n{updateErrorMessage}",
					"Oh dear");
			}

			_updateTask = null;
		}

		private void UpdatePageContent()
		{
			if (LocationAndCacheData.IsCacheDataValid)
			{
				// CachedTemperature, CachedCondition
				HeaderText.Text = string.Format("{0}, {1}", LocationAndCacheData.ReadableTemperature, LocationAndCacheData.ConditionDescription);
				// Location
				SubHeaderText.Text = LocationAndCacheData.LocationLabel;
				CurrentConditionIcon.Visibility = Visibility.Visible;
				CurrentConditionIcon.Glyph = LocationAndCacheData.ConditionWeatherIconsGlyph;
				// Content tabs: Chart / LocationAndCacheData
				ContentChart.Invalidate();
				ContentData.Text = GetContentDataTabText();
				// Last updated
				LastUpdateTimeText.Text = string.Format("Updated {0}", DateTimeUtils.GetRelativeTimeString(LocationAndCacheData.CacheLastUpdateTime));
				ToolTipService.SetToolTip(
					LastUpdateTimeText,
					LocationAndCacheData.CacheLastUpdateTime?.ToLocalTime().ToString("G"));
			}
			else
			{
				// No data at all
				// There should always be at least some cache, no matter how valid.
				// This means that app is started a first time or settings were cleared.

				// CachedCondition - unknown
				HeaderText.Text = "No data";
				// Location - always known
				SubHeaderText.Text = LocationAndCacheData.LocationLabel;
				CurrentConditionIcon.Visibility = Visibility.Collapsed;
				// Content tabs: Chart / LocationAndCacheData
				ContentChart.Invalidate();
				ContentData.Text = string.Empty;
				// Last updated - unknown
				LastUpdateTimeText.Text = string.Empty;
				ToolTipService.SetToolTip(LastUpdateTimeText, null);
			}
		}

		private void ContentChartCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
		{
			var ds = args.DrawingSession;

			// Clear the canvas first
			ds.Clear(Colors.Transparent);

			// No data
			if (!LocationAndCacheData.IsCacheDataValid)
				return;
			// Insufficient data
			var hourlyData = LocationAndCacheData.GetHourlyDataSinceNow();
			if (hourlyData.Count <= 2)
				return;

			// Call Charting class to do the actual drawing
			Charting.DrawHomePageChart(sender, ds, hourlyData);
		}

		private static string GetContentDataTabText()
		{
			var tabContent = new StringBuilder();

			tabContent.AppendLine($"Last update: {LocationAndCacheData.CacheLastUpdateTime?.ToLocalTime().ToString("G")} ({LocationAndCacheData.CacheLastUpdateTime?.UtcDateTime.ToString("u")})");
			tabContent.AppendLine($"Valid until: {LocationAndCacheData.CacheValidUntil?.ToLocalTime().ToString("G")} ({LocationAndCacheData.CacheValidUntil?.UtcDateTime.ToString("u")})");
			tabContent.AppendLine($"Location ID: {LocationAndCacheData.CacheLocationID}");
			tabContent.AppendLine($"Provider ID: {LocationAndCacheData.CacheWeatherProviderID} ({WeatherProvider.Get(LocationAndCacheData.CacheWeatherProviderID ?? -1).ProviderName})");
			tabContent.AppendLine();

			tabContent.Append(string.Join(Environment.NewLine, LocationAndCacheData.GetHourlyDataSinceNow()));

			return tabContent.ToString();
		}

		#region User Interface
		private void ContentSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
		{
			if (sender.SelectedItem == ContentSelectorBar_ChartCard)
			{
				ContentChart.Visibility = Visibility.Visible;
				ContentData.Visibility = Visibility.Collapsed;
				Scroller.IsEnabled = false;
			}
			else
			{
				ContentChart.Visibility = Visibility.Collapsed;
				ContentData.Visibility = Visibility.Visible;
				Scroller.IsEnabled = true;
			}
		}
		#endregion

		#region Utils
		private void ToggleDarkAndLightTheme()
		{
			var systemTheme = ThemeUtils.GetSystemTheme(); // Dark or Light (only, OS is always specific)
			
			ElementTheme newTheme;
			if (Preferences.AppTheme == ElementTheme.Default)
			{
				// Is using OS default
				newTheme = systemTheme.Opposite(); // Set the opposite as override
			}
			else
			{
				// Is using specific theme override
				if (Preferences.AppTheme == systemTheme)
				{
					// That is specific but matches OS default
					newTheme = systemTheme.Opposite(); // Set the opposite
				}
				else
				{
					// That is opposite to OS default
					newTheme = ElementTheme.Default; // Set the OS default
				}
			}
			
			Preferences.SetAppTheme(newTheme);
			WindowUtils.ApplyAppTheme(App.MainWindow);
			ContentChart.Invalidate();
		}
		#endregion
	}
}
