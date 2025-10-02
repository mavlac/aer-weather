using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Aer
{
	public sealed partial class MainWindow : Window
	{
		private const int DefaultWindowWidth = 1280;
		private const int DefaultWindowHeight = 800;

		public MainWindow()
		{
			InitializeComponent();

			// Saving and restoring window size and position
			WindowPlacementManager.Restore(this, DefaultWindowWidth, DefaultWindowHeight);
			this.Closed += (s, e) => WindowPlacementManager.Save(this);

			// Saving and restoring the nav pane
			NavigationViewStateManager.Restore(NavView, false);
			this.Closed += (s, e) => NavigationViewStateManager.Save(NavView);

			// Update back button and nav highlight when navigation happens
			ContentFrame.Navigated += ContentFrame_Navigated;
			ContentFrame.Navigate(typeof(MainPage));
		}

		private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			// Enable or disable back button
			NavView.IsBackEnabled = ContentFrame.CanGoBack;

			// Highlight the correct menu item
			if (e.SourcePageType == typeof(MainPage))
			{
				NavView.SelectedItem = NavView.MenuItems
					.OfType<NavigationViewItem>()
					.FirstOrDefault(x => (string)x.Tag == MainPage.NavigationViewItemTag);
			}
			else
			{
				NavView.SelectedItem = NavView.SettingsItem;
			}
		}

		// Handle menu item selection
		private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			if (args.IsSettingsSelected)
			{
				ContentFrame.Navigate(typeof(SettingsPage));
			}
			else
			{
				if (args.SelectedItem is NavigationViewItem selectedItem)
				{
					switch (selectedItem.Tag)
					{
						case MainPage.NavigationViewItemTag:
							ContentFrame.Navigate(typeof(MainPage));
							break;
					}
				}
			}
		}

		// Handle Back button
		private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
		{
			if (ContentFrame.CanGoBack)
			{
				ContentFrame.GoBack();
			}
		}
	}
}
