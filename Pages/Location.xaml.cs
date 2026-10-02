using Aer.Data;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Aer.Pages
{
	public sealed partial class Location : Page
	{
		public Location()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

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
	}
}
