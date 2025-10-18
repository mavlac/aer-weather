using Aer.Drawing;
using Aer.Utils;
using Aer.Weather;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
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

		private Task<bool>? _updateTask;
		private CancellationTokenSource? _updateTaskCts;

		public HomePage()
		{
			InitializeComponent();

			// This should be immediate, but can contain default location with no weather data
			Data.LoadCacheOrDefaults();

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

			UpdateDataFromNetwork(forceNetworkUpdate: false);
		}

		protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			
			_minuteTimer.Stop();

			_updateTaskCts?.Cancel();
			_updateTaskCts?.Dispose();
			_updateTaskCts = null;
		}

		private async void UpdateDataFromNetwork(bool forceNetworkUpdate)
		{
			if (_updateTask != null)
				return;

			// Cancel any previous operation first
			_updateTaskCts?.Cancel();
			_updateTaskCts?.Dispose();

			_updateTaskCts = new();
			var cancellationToken = _updateTaskCts.Token;

			// Kick off the network update, but don't await it yet
			_updateTask = Data.UpdateWeatherDataFromNetwork(forceNetworkUpdate, cancellationToken);

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
			bool didUpdateSucceed = await _updateTask;

			// Whatever happened, does not matter, hide loader
			LoadingOverlay.Visibility = Visibility.Collapsed;
			UpdatePageContent(); // Will update the content and LastUpdateTimeText

			if (!didUpdateSucceed)
			{
				await MessageBoxEx.ShowAsync(
					"Unable to update weather data",
					"Could not load new weather data from the network.\r\nPlease check your internet connection and restart the app.",
					"Oh dear");
			}

			_updateTask = null;
		}

		private void OnMinuteTick()
		{
			UpdatePageContent(); // Among other content will update the LastUpdateTimeText

			// Data Update
			// If cache is still valid, no network call will be made.
			UpdateDataFromNetwork(false);
		}

		private void UpdatePageContent()
		{
			if (Data.IsCacheDataValid)
			{
				// CachedTemperature, CachedCondition
				HeaderText.Text = string.Format("{0}, {1}", Data.ReadableTemperature, Data.ConditionDescription);
				// Location
				SubHeaderText.Text = Data.LocationLabel;
				SubHeaderIcon.Visibility = Visibility.Visible;
				SubHeaderIcon.Glyph = Data.ConditionWeatherIconsGlyph;
				// Content
				ContentChart.Invalidate();
				ContentData.Text = string.Join(Environment.NewLine, Data.GetHourlyDataSinceNow());
				// Last updated
				LastUpdateTimeText.Text = string.Format("Updated {0}", DateTimeUtils.GetRelativeTimeString(Data.CacheLastUpdateTime));
			}
			else
			{
				// No data at all
				// There should always be at least some cache, no matter how valid.
				// This means that app is started a first time or settings were cleared.

				// CachedCondition - unknown
				HeaderText.Text = "No data";
				// Location - always known
				SubHeaderText.Text = Data.LocationLabel;
				SubHeaderIcon.Visibility = Visibility.Collapsed;
				// Content
				ContentChart.Invalidate();
				ContentData.Text = string.Empty;
				// Last updated - unknown
				LastUpdateTimeText.Text = string.Empty;
			}
		}

		private void ContentChartCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
		{
			var ds = args.DrawingSession;

			ds.Clear(Colors.Transparent);

			var hourlyData = Data.GetHourlyDataSinceNow();

			// No data or not enough data - just clean
			if (!Data.IsCacheDataValid || hourlyData.Count <= 2)
				return;

			// Call Charting class to do the actual drawing
			Charting.DrawHomePageChart(sender, ds, hourlyData);
		}

		private void ContentSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
		{
			if (sender.SelectedItem == ContentSelectorBar_ChartCard)
			{
				ContentChart.Visibility = Visibility.Visible;
				ContentData.Visibility = Visibility.Collapsed;
			}
			else
			{
				ContentChart.Visibility = Visibility.Collapsed;
				ContentData.Visibility = Visibility.Visible;
			}
		}

		private void LastUpdateTimeTextHyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
		{
			UpdateDataFromNetwork(forceNetworkUpdate: true);
		}
	}
}
