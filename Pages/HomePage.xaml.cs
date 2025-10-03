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
			if (Data.IsValid)
			{
				// TODO
				SubHeaderText.Text = Data.LocationName;
			}
			else
			{
				HeaderText.Text = "No data";
				SubHeaderText.Text = "Please check your internet connection and restart the app.";
				return;
			}
		}
	}
}
