using Microsoft.UI.Xaml.Controls;

namespace Aer
{
	public sealed partial class HomePage : Page
	{
		private const string HeaderFormat = "{0}, {1} °C";
		private const string LastUpdateTimeFormat = "last updated {0}";

		public HomePage()
		{
			InitializeComponent();

			UpdatePageDataContent();
			Data.Updated += Data_Updated;
		}

		private void Data_Updated()
		{
			UpdatePageDataContent();
		}

		private void UpdatePageDataContent()
		{
			if (Data.IsWeatherDataValid)
			{
				HeaderText.Text = string.Format(HeaderFormat, Data.Condition, Data.Temperature);
				SubHeaderText.Text = Data.LocationLabel;
				LastUpdateTimeText.Text = string.Format(LastUpdateTimeFormat, Data.LastUpdateTime);
				
				// TODO: Draw the chart
			}
			else
			{
				HeaderText.Text = "No data";
				SubHeaderText.Text = "Please check your internet connection and restart the app.";
				LastUpdateTimeText.Text = string.Empty;
				return;
			}
		}
	}
}
