using Aer.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Windows.ApplicationModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Aer
{
	public sealed partial class MainWindow : Window
	{
		private const int DefaultWindowWidth = 1600;
		private const int DefaultWindowHeight = 1024;

		public string WindowTitle => Package.Current.DisplayName;

		public MainWindow()
		{
			InitializeComponent();

			WindowUtils.ApplyAppTheme(this);

			this.SizeChanged += MainWindow_SizeChanged;
			this.Closed += Window_Closed;

			WindowPlacementManager.Restore(this, DefaultWindowWidth, DefaultWindowHeight);
			NavigationViewStateManager.Restore(NavView, false);

			ContentFrame.Navigated += ContentFrame_Navigated;
			ContentFrame.Navigate(typeof(HomePage));

			// Run after first layout pass of the visual tree
			RootGrid.LayoutUpdated += RootGrid_LayoutUpdatedOnce;
		}

		private void RootGrid_LayoutUpdatedOnce(object? sender, object e)
		{
			// LayoutUpdated fires after measure / arrange - so element sizes & positions are valid.
			// By attaching it to the root element(a Grid, StackPanel, etc.), it's sure it runs when the window content has stabilized.
			RootGrid.LayoutUpdated -= RootGrid_LayoutUpdatedOnce;

			// Wait one UI tick so NavigationView finishes its internal layout
			DispatcherQueue.TryEnqueue(() =>
			{
				WindowUtils.UpdateTitleBarDraggableArea(this);
			});
		}

		private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			WindowUtils.UpdateTitleBarDraggableArea(this);

			// Nav Pane Responsivity
			if (e.Size.Width < (double)Application.Current.Resources["Breakpoint840Plus"])
			{
				NavView.OpenPaneLength = (double)Application.Current.Resources["NavPaneNarrowWidth"];
			}
			else
			{
				NavView.ClearValue(NavigationView.OpenPaneLengthProperty); // restore default (320)
			}
		}

		private void Window_Closed(object sender, WindowEventArgs args)
		{
			// Unsubscribe from events to allow proper cleanup
			this.SizeChanged -= MainWindow_SizeChanged;
			ContentFrame.Navigated -= ContentFrame_Navigated;

			WindowPlacementManager.Save(this);
			NavigationViewStateManager.Save(NavView);
			Preferences.Save();
		}

		// Handle Nav selection
		private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			if (args.IsSettingsSelected)
			{
				NavigateToSettingsPage();
			}
			else
			{
				if (args.SelectedItem is NavigationViewItem selectedItem)
				{
					if ((string)selectedItem.Tag == (string)Application.Current.Resources["HomePageNavigationTag"])
					{
						ContentFrame.Navigate(typeof(HomePage));
					}
				}
			}
		}

		private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			// Enable or disable back button
			NavView.IsBackEnabled = ContentFrame.CanGoBack;

			// Highlight the correct menu item
			if (e.SourcePageType == typeof(HomePage))
			{
				NavView.SelectedItem = NavView.MenuItems
					.OfType<NavigationViewItem>()
					.FirstOrDefault(x => (string)x.Tag == (string)Application.Current.Resources["HomePageNavigationTag"]);
			}
			else
			{
				NavView.SelectedItem = NavView.SettingsItem;
			}
		}

		// Handle Back button (Now hidden)
		private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
		{
			if (ContentFrame.CanGoBack)
			{
				ContentFrame.GoBack();
			}
		}

		public void NavigateToSettingsPage()
		{
			ContentFrame.Navigate(typeof(SettingsPage));
		}
	}
}
