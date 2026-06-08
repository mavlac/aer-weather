using Aer.Data;
using Aer.Utils;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Linq;
using Windows.ApplicationModel;
using Windows.System;
using Windows.UI.Core;
using WindowActivatedEventArgs = Microsoft.UI.Xaml.WindowActivatedEventArgs;
using WindowSizeChangedEventArgs = Microsoft.UI.Xaml.WindowSizeChangedEventArgs;

namespace Aer
{
	public sealed partial class MainWindow : Window
	{
		public enum GlobalHotkey
		{
			OpenSettings,
			DarkThemeToggle
		}

		private bool _isKeyHandlerAdded;

		public string WindowTitle => Package.Current.DisplayName;

		public static event Action<GlobalHotkey>? GlobalHotkeyPressed;
		public static event Action<Windows.Foundation.Size>? WindowSizeChanged;

		public MainWindow()
		{
			InitializeComponent();

			WindowUtils.ApplyAppTheme(this);

			this.Activated += MainWindow_Activated;
			this.SizeChanged += MainWindow_SizeChanged;
			this.Closed += MainWindow_Closed;

			// Load size and position
			WindowPlacementManager.Restore(this, (int)Application.Current.Resources["DefaultWindowWidth"], (int)Application.Current.Resources["DefaultWindowHeight"]);
			// Load nav state
			NavigationViewStateManager.Restore(NavView, false);

			HomeNavItem.Tag = HomePage.NavigationTag;

			ContentFrame.Navigated += ContentFrame_Navigated;
			ContentFrame.Navigate(typeof(HomePage));

			// Run after first layout pass of the visual tree
			RootGrid.LayoutUpdated += RootGrid_LayoutUpdatedOnce;
		}

		private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
		{
			// Only act when the window is becoming active (not deactivated)
			if (args.WindowActivationState == WindowActivationState.Deactivated)
				return;

			// Ensure Content is ready and handler isn't already added
			if (this.Content != null && !_isKeyHandlerAdded)
			{
				this.Content?.AddHandler(
				UIElement.KeyDownEvent,
				new KeyEventHandler(OnKeyDown),
				true); // Handle even if handled elsewhere

				_isKeyHandlerAdded = true;
			}
		}

		private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			WindowUtils.UpdateTitleBarDraggableArea(this);

			// Nav Pane Responsivity
			if (e.Size.Width < (double)Application.Current.Resources["Breakpoint1008Plus"])
			{
				NavView.OpenPaneLength = (double)Application.Current.Resources["NavPaneNarrowWidth"];
			}
			else
			{
				NavView.ClearValue(NavigationView.OpenPaneLengthProperty); // restore default (320)
			}

			WindowSizeChanged?.Invoke(e.Size);
		}

		private void MainWindow_Closed(object sender, WindowEventArgs args)
		{
			// Unsubscribe from events to allow proper cleanup
			this.SizeChanged -= MainWindow_SizeChanged;
			this.Closed -= MainWindow_Closed;
			ContentFrame.Navigated -= ContentFrame_Navigated;

			GlobalHotkeyPressed = null;

			WindowPlacementManager.Save(this);
			NavigationViewStateManager.Save(NavView);
			
			Preferences.Save();
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

		public void NavigateToSettingsPage(bool focusLocationSearch = false)
		{
			ContentFrame.Navigate(typeof(SettingsPage), new SettingsNavigationArgs { FocusLocationSearch = focusLocationSearch });
		}

		public class SettingsNavigationArgs
		{
			public bool FocusLocationSearch { get; set; }
		}
		#endregion

		#region Hotkeys
		private void OnKeyDown(object sender, KeyRoutedEventArgs e)
		{
			// Ctrl + Alt + S
			if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down) &&
				InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down) &&
				e.Key == VirtualKey.S)
			{
				e.Handled = true;
				GlobalHotkeyPressed?.Invoke(GlobalHotkey.OpenSettings);
			}

			// Ctrl + D
			if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down) &&
				e.Key == VirtualKey.D)
			{
				e.Handled = true;
				GlobalHotkeyPressed?.Invoke(GlobalHotkey.DarkThemeToggle);
			}
		}
		#endregion
	}
}
