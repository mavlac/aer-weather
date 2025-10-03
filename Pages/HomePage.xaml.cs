using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Aer
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class HomePage : Page
	{
		private const string HeaderFormat = "{0}, {1} °C";
		private const string LastUpdateTimeFormat = "last updated {0}";

		public HomePage()
		{
			InitializeComponent();

			UpdatePageDataContent();
			Data.Updated += Data_Loaded;
		}

		private void Data_Loaded()
		{
			UpdatePageDataContent();
		}

		private void UpdatePageDataContent()
		{
			if (Data.IsWeatherDataValid)
			{
				HeaderText.Text = string.Format(HeaderFormat, Data.Condition, Data.Temperature);
				SubHeaderText.Text = Data.LocationName;
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
