using Aer.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Windows.ApplicationModel;

namespace Aer
{
	public sealed partial class MainWindow : Window
	{
		private const int DefaultWindowWidth = 1660;
		private const int DefaultWindowHeight = 1082;

		public string WindowTitle => Package.Current.DisplayName;

		public MainWindow()
		{
			InitializeComponent();

			WindowUtils.ApplyAppTheme(this);

			this.SizeChanged += MainWindow_SizeChanged;
			this.Closed += MainWindow_Closed;

			WindowPlacementManager.Restore(this, DefaultWindowWidth, DefaultWindowHeight); // Load size and position
			NavigationViewStateManager.Restore(NavView, false); // Load nav state

			HomeNavItem.Tag = HomePage.NavigationTag;

			ContentFrame.Navigated += ContentFrame_Navigated;
			ContentFrame.Navigate(typeof(HomePage));

			// Run after first layout pass of the visual tree
			RootGrid.LayoutUpdated += RootGrid_LayoutUpdatedOnce;
		}

		private void RootGrid_LayoutUpdatedOnce(object? sender, object e)
		{
			// LayoutUpdated fires after measure / arrange - so element sizes & positions are now valid.
			// By attaching event to the root element(a Grid, StackPanel, etc.), it's sure following code runs when the window content has stabilized.
			RootGrid.LayoutUpdated -= RootGrid_LayoutUpdatedOnce;

			// Wait one UI tick so NavigationView finishes its internal layout
			DispatcherQueue.TryEnqueue(() =>
			{
				WindowUtils.UpdateTitleBarDraggableArea(this); // Now finally is the time to update the title bar
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

		private void MainWindow_Closed(object sender, WindowEventArgs args)
		{
			// Unsubscribe from events to allow proper cleanup
			this.SizeChanged -= MainWindow_SizeChanged;
			this.Closed -= MainWindow_Closed;
			ContentFrame.Navigated -= ContentFrame_Navigated;

			WindowPlacementManager.Save(this);
			NavigationViewStateManager.Save(NavView);
			
			Preferences.Save();
		}
		
		#region Navigation
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
					if ((string)selectedItem.Tag == HomePage.NavigationTag)
					{
						ContentFrame.Navigate(typeof(HomePage));
					}
				}
			}
		}

		private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
		{
			if (ContentFrame.CanGoBack)
			{
				ContentFrame.GoBack();
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
					.FirstOrDefault(x => (string)x.Tag == HomePage.NavigationTag);
			}
			else
			{
				NavView.SelectedItem = NavView.SettingsItem;
			}
		}

		public void NavigateToSettingsPage()
		{
			// TODO: Param to focus the Location AutoSuggestBox
			ContentFrame.Navigate(typeof(SettingsPage));
		}
		#endregion
	}
}
