using Aer.Data;
using Aer.Drawing;
using Aer.Utils;
using Aer.Weather;
using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.BadgeNotifications;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Aer
{
	public sealed partial class HomePage : Page
	{
		public const string NavigationTag = "home";

		private readonly MinuteTimer _minuteTimer = new();
		private bool _isMouseDownOverChart = false;

		private Task<(bool status, string message)>? _updateTask;
		private CancellationTokenSource? _updateTaskCts;

		public HomePage()
		{
			InitializeComponent();
			
			Location.LoadOrSetDefaults();
			
			Loading += HomePage_Loading;
			Loaded += HomePage_Loaded;
			
			_minuteTimer.Start(OnMinuteTick);
		}

		private async void HomePage_Loading(FrameworkElement sender, object args)
		{
			if (!Preferences.WasWelcomeShown)
			{
				Preferences.SetWelcomeShown(true);

				bool proceedToSettings = await MessageBoxEx.ShowAsync(
					$"Welcome to {Package.Current.DisplayName}!",
					"Thank you for using my weather app.\r\n\r\nThe default location is shown for now.\r\nSet your preferred location in Settings.",
					primaryButtonText: "Open Settings");

				if (proceedToSettings)
				{
					App.MainWindow.NavigateToSettingsPage(true);
				}
			}
		}

		private async void HomePage_Loaded(object sender, RoutedEventArgs e)
		{
			// Show whatever is available immediately
			// Can be default location with no weather data, can be old cached data, can be valid current data
			UpdatePageContent();

			await RefreshData();
			UpdatePageContent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			MainWindow.GlobalHotkeyPressed += MainWindow_GlobalHotkeyPressed;
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			
			_minuteTimer.Stop();

			_updateTaskCts?.Cancel();
			_updateTaskCts?.Dispose();
			_updateTaskCts = null;

			MainWindow.GlobalHotkeyPressed -= MainWindow_GlobalHotkeyPressed;
		}

		private void MainWindow_GlobalHotkeyPressed(MainWindow.GlobalHotkey obj)
		{
			switch(obj)
			{
				case MainWindow.GlobalHotkey.OpenSettings:
					App.MainWindow.NavigateToSettingsPage(false);
					break;

				case MainWindow.GlobalHotkey.DarkThemeToggle:
					Preferences.ToggleDarkAndLightTheme();
					ContentChart.Invalidate();
					break;
			}
		}

		private async void OnMinuteTick()
		{
			await RefreshData();
			UpdatePageContent();
		}

		private async Task RefreshData()
		{
			if (_updateTask != null)
				return;

			// Cancel any previous operation first
			_updateTaskCts?.Cancel();
			_updateTaskCts?.Dispose();

			_updateTaskCts = new();
			// Link local cancellation with app shutdown token so shutdown cancels the operation as well
			var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_updateTaskCts.Token, App.ShutdownToken);
			var cancellationToken = linkedTokenSource.Token;

			// Kick off the network update, but don't await it yet
			_updateTask = WeatherDataManager.Load(cancellationToken);

			// Wait briefly before deciding to show loader, cancellable
			try
			{
				await Task.Delay(200, cancellationToken);
			}
			catch (TaskCanceledException)
			{
				// Ensure linkedTokenSource CTS is disposed
				linkedTokenSource.Dispose();
				_updateTask = null;
				return;
			}

			// Only show loader and loading status if still not finished
			if (!_updateTask.IsCompleted)
			{
				BadgeNotificationManager.Current.ClearBadge();
				BadgeNotificationManager.Current.SetBadgeAsGlyph(BadgeNotificationGlyph.Activity);
				
				LoadingOverlay.Visibility = Visibility.Visible;
				LastUpdateTimeText.Text = $"Loading from {WeatherProvider.Get(Preferences.WeatherProviderId!.Value).ProviderURL}\u2026";
			}

			// Await completion (still necessary to observe exceptions etc.)
			var (didUpdateSucceed, updateErrorMessage) = await _updateTask;

			// Dispose linkedTokenSource token source now that work is done
			linkedTokenSource.Dispose();

			// Whatever happened, does not matter, hide loader
			LoadingOverlay.Visibility = Visibility.Collapsed;

			// Simulate a network error
			//didUpdateSucceed = false;
			//updateErrorMessage = "Simulated network error for demonstration purposes.";

			if (!didUpdateSucceed)
			{
				await DispatcherQueue.EnqueueAsync(() =>
				{
					BadgeNotificationManager.Current.ClearBadge();
					BadgeNotificationManager.Current.SetBadgeAsGlyph(BadgeNotificationGlyph.Error);
				});

				await MessageBoxEx.ShowAsync(
					"Unable to update weather data",
					"Could not load forecast data update from the network.\r\n"
						+ $"The {WeatherProvider.Get(Preferences.WeatherProviderId!.Value).ProviderName} service may be temporarily unavailable, or unreachable using your current network connection.\r\n"
						+ $"\r\n{updateErrorMessage}",
					"Oh dear");
			}
			else
			{
				BadgeNotificationManager.Current.ClearBadge();
			}

			_updateTask = null;
		}

		private void UpdatePageContent()
		{
			if (WeatherDataManager.IsWeatherDataLoaded)
			{
				// Temperature, Condition, Icon
				if (_isMouseDownOverChart == false)
				{
					// Normal
					HeaderText.Text = $"{WeatherDataManager.WeatherData.ReadableTemperature}, {WeatherDataManager.WeatherData.ConditionDescription}";
					ToolTipService.SetToolTip(HeaderText, $"Feels like: {WeatherDataManager.WeatherData.ReadableApparentTemperature}");
					CurrentConditionIcon.Visibility = Visibility.Visible;
					CurrentConditionIcon.Glyph = WeatherDataManager.WeatherData.ConditionWeatherIconsGlyph;
				}
				else
				{
					// Feels-like
					HeaderText.Text = $"{WeatherDataManager.WeatherData.ReadableApparentTemperature} (feels like)";
					ToolTipService.SetToolTip(HeaderText, null);
					CurrentConditionIcon.Visibility = Visibility.Collapsed;
				}
				// Location
				SubHeaderText.Text = Location.Label;
				// Content tabs: Chart / LocationAndCacheData
				ContentChart.Invalidate();
				ContentData.Text = GetContentDataTabText();
				// Last updated
				LastUpdateTimeText.Text = $"Updated {DateTimeUtils.GetRelativeTimeString(WeatherDataManager.WeatherData.Created)}";
				ToolTipService.SetToolTip(
					LastUpdateTimeText,
					WeatherDataManager.WeatherData.Created.ToLocalTime().ToString("G"));
			}
			else
			{
				// No data at all
				// There should always be at least some cache, no matter how valid.
				// This means that app is started a first time or settings were cleared.

				// Condition - unknown
				HeaderText.Text = "No data";
				ToolTipService.SetToolTip(HeaderText, null);
				// Location - always known
				SubHeaderText.Text = Location.Label;
				CurrentConditionIcon.Visibility = Visibility.Collapsed;
				// Content tabs: Chart / LocationAndCacheData
				ContentChart.Invalidate();
				ContentData.Text = string.Empty;
				// Last updated - unknown
				LastUpdateTimeText.Text = string.Empty;
				ToolTipService.SetToolTip(LastUpdateTimeText, null);
			}
		}

		#region Canvas
		private void ContentChart_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
		{
			var ds = args.DrawingSession;

			// Clear the canvas first
			ds.Clear(Colors.Transparent);

			// No data
			if (!WeatherDataManager.IsWeatherDataLoaded)
				return;

			// Insufficient data
			var hourlyData = WeatherDataManager.GetHourlyDataSinceNow();
			if (hourlyData.Count <= 2)
				return;

			// Call Charting class to do the actual drawing
			Charting.DrawHomePageChart(sender, ds, hourlyData, _isMouseDownOverChart);
		}

		private void ContentChart_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			Debug.WriteLine("PointerPressed on Chart");
			if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
			{
				var point = e.GetCurrentPoint(ContentChart);

				if (point.Properties.IsLeftButtonPressed)
				{
					_isMouseDownOverChart = true;
					UpdatePageContent();
					ContentChart.CapturePointer(e.Pointer); // important if user drags outside
				}
			}
		}

		private void ContentChart_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			_isMouseDownOverChart = false;
			UpdatePageContent();
			ContentChart.ReleasePointerCapture(e.Pointer);
		}

		private void ContentChart_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			_isMouseDownOverChart = false;
			ContentChart.Invalidate();
			ContentChart.ReleasePointerCapture(e.Pointer);
		}
		#endregion

		private static string GetContentDataTabText()
		{
			var tabContent = new StringBuilder();

			tabContent.AppendLine($"Location ID: {WeatherDataManager.WeatherData.LocationID}");
			tabContent.AppendLine($"Provider ID: {WeatherDataManager.WeatherData.WeatherProviderID} ({WeatherProvider.Get(WeatherDataManager.WeatherData.WeatherProviderID).ProviderName})");
			tabContent.AppendLine($"Last update: {WeatherDataManager.WeatherData.Created.ToLocalTime().ToString("G")} ({WeatherDataManager.WeatherData.Created.UtcDateTime.ToString("u")})");
			tabContent.AppendLine($"Valid until: {WeatherDataManager.WeatherData.ValidUntil.ToLocalTime().ToString("G")} ({WeatherDataManager.WeatherData.ValidUntil.UtcDateTime.ToString("u")})");
			tabContent.AppendLine();

			tabContent.Append(string.Join(Environment.NewLine, WeatherDataManager.GetHourlyDataSinceNow()));

			return tabContent.ToString();
		}

		#region User Interface
		private void SubHeaderTextHyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
		{
			App.MainWindow.NavigateToSettingsPage(true);

			/*
			var flyout = new MenuFlyout();

			// TODO: The FlyOut content

			var currentItem = new RadioMenuFlyoutItem
			{
				Text = "Prague, CZ",
				GroupName = "RecentLocations",
				IsChecked = true
			};
			currentItem.Click += MenuItem_Click;
			flyout.Items.Add(currentItem);

			// Other items
			var berlin = new RadioMenuFlyoutItem
			{
				Text = "Berlin, DE",
				GroupName = "RecentLocations"
			};
			berlin.Click += MenuItem_Click;
			flyout.Items.Add(berlin);

			var vienna = new RadioMenuFlyoutItem
			{
				Text = "Vienna, AU",
				GroupName = "RecentLocations"
			};
			vienna.Click += MenuItem_Click;
			flyout.Items.Add(vienna);

			flyout.Items.Add(new MenuFlyoutSeparator());

			var openSettingsItem = new MenuFlyoutItem
			{
				Text = "Location Settings...",
				KeyboardAccelerators =
				{
					new Microsoft.UI.Xaml.Input.KeyboardAccelerator
					{
						Key = Windows.System.VirtualKey.S,
						Modifiers = Windows.System.VirtualKeyModifiers.Control |
									Windows.System.VirtualKeyModifiers.Menu // Alt
					}
				}
			};
			openSettingsItem.Click += (_, _) => App.MainWindow.NavigateToSettingsPage(true);

			flyout.Items.Add(openSettingsItem);

			flyout.ShowAt(
				SubHeaderTextBlock,
				new FlyoutShowOptions
				{
					Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft
				});
			*/
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (sender is RadioMenuFlyoutItem item)
			{
				// PROOF it works
				System.Diagnostics.Debug.WriteLine($"Clicked: {item.Text}");
			}
		}

		private void ContentSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
		{
			if (sender.SelectedItem == ContentSelectorBar_ChartCard)
			{
				ContentChart.Visibility = Visibility.Visible;
				ContentData.Visibility = Visibility.Collapsed;

				Scroller.VerticalScrollMode = ScrollMode.Disabled;
				Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
			}
			else
			{
				ContentChart.Visibility = Visibility.Collapsed;
				ContentData.Visibility = Visibility.Visible;

				Scroller.VerticalScrollMode = ScrollMode.Auto;
				Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			}
		}
		#endregion
	}
}
