using Aer.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Aer
{
	public sealed partial class HomePage : Page
	{
		public const string NavigationTag = "home";

		private readonly MinuteTimer _minuteTimer = new();

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
			UpdatePageDataContent();

			Data.UpdatedFromNetwork += Data_UpdatedFromNetwork;

			UpdateDataFromNetwork(forceNetworkUpdate: false);
		}

		protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			
			_minuteTimer.Stop();
		}

		private void OnMinuteTick()
		{
			UpdatePageDataContent(); // Among other content will update the LastUpdateTimeText

			// Data Update
			// If cache is still valid, no network call will be made.
			UpdateDataFromNetwork(false);
		}

		private async void UpdateDataFromNetwork(bool forceNetworkUpdate)
		{
			// Kick off the network update, but don't await it yet
			var updateTask = Data.UpdateWeatherDataFromNetwork(forceNetworkUpdate);

			// Wait briefly before deciding to show loader
			await Task.Delay(200);

			// Only show loader if still not finished
			if (!updateTask.IsCompleted)
			{
				LoadingOverlay.Visibility = Visibility.Visible;
			}

			// Await completion (still necessary to observe exceptions etc.)
			bool updateSucceeded = await updateTask;

			// Whatever happened, does not matter, hide loader
			LoadingOverlay.Visibility = Visibility.Collapsed;

			if (!updateSucceeded)
			{
				await MessageBoxEx.ShowAsync(
					"Unable to update weather data",
					"Could not load new weather data from the network.\r\nPlease check your internet connection and restart the app.",
					"Oh dear");
			}
		}

		private void Data_UpdatedFromNetwork()
		{
			UpdatePageDataContent();
		}

		private void UpdatePageDataContent()
		{
			if (Data.IsCacheDataValid)
			{
				// CachedTemperature, CachedCondition
				HeaderText.Text = string.Format("{0}, {1}", Data.ReadableTemperature, Data.ReadableCondition);
				// Location
				SubHeaderText.Text = Data.LocationLabel;
				SubHeaderIcon.Visibility = Visibility.Visible;
				SubHeaderIcon.Glyph = Data.ConditionWeatherIconsGlyph;
				// Content
				// TODO: Draw the chart
				string contentTempText = string.Empty;
				for (int i = 0; i < Data.CachedHourly.Count; i++)
					contentTempText += $"{Data.CachedHourly[i].Time} : {Data.CachedHourly[i].ConditionCode}, {Data.CachedHourly[i].IsDaytime}\n";
				Content.Text = contentTempText;
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
				Content.Text = string.Empty;
				// Last updated - unknown
				LastUpdateTimeText.Text = "Loading from network…";
			}
		}

		private void LastUpdateTimeTextHyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
		{
			UpdateDataFromNetwork(forceNetworkUpdate: true);
		}
	}
}
