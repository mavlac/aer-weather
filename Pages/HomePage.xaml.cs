using Aer.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Aer
{
	public sealed partial class HomePage : Page
	{
		public HomePage()
		{
			InitializeComponent();
			
			// This should be immediate, but can contain default location with no weather data
			Data.LoadLastSavedValues();
			
			Loaded += HomePage_Loaded;
		}

		private async void HomePage_Loaded(object sender, RoutedEventArgs e)
		{
			// Show whatever is available immediately
			// Can be default location with no weather data, can be old cached data, can be valid current data
			UpdatePageDataContent();

			Data.UpdatedFromNetwork += Data_UpdatedFromNetwork;

			// Kick off the network update, but don't await it yet
			var updateTask = Data.UpdateWeatherDataFromNetwork();

			// Wait briefly before deciding to show loader
			await Task.Delay(200);

			// Only show loader if still not finished
			if (!updateTask.IsCompleted)
			{
				LoadingOverlay.Visibility = Visibility.Visible;
			}

			// Await completion (still necessary to observe exceptions etc.)
			bool wasUpdated = await updateTask;

			// If nothing was wasUpdated or it finished, does not matter, hide loader
			LoadingOverlay.Visibility = Visibility.Collapsed;
		}

		private void Data_UpdatedFromNetwork()
		{
			UpdatePageDataContent();
		}

		private void UpdatePageDataContent()
		{
			if (Data.IsWeatherDataLoaded)
			{
				// Condition
				HeaderText.Text = string.Format("{0}, {1} {2}", Data.Condition, Data.Temperature, Preferences.TemperatureUnits.ToUnitString());
				// Location
				SubHeaderText.Text = Data.LocationLabel;
				// Last updated
				LastUpdateTimeText.Text = string.Format("Updated {0}", DateTimeUtils.GetRelativeTimeString(Data.LastUpdateTime));
				
				// TODO: Draw the chart
			}
			else
			{
				// No data at all
				// There should always be at least some cache, no matter how valid.
				// This means that app is started first time ever or settings were cleared.

				// Condition unknown
				HeaderText.Text = "No data";
				// Location
				SubHeaderText.Text = Data.LocationLabel;
				// Last updated unknown
				LastUpdateTimeText.Text = "Loading from network…";
			}
		}
	}
}
