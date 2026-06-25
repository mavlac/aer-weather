using Aer.Data;
using Aer.Utils;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Aer
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public partial class App : Application
	{
		internal static MainWindow MainWindow { get; private set; } = null!;
		
		/// <summary>
		/// Remember the state of application start, so when accent color setting is changed, restart button can appear.
		/// </summary>
		internal static bool StartedUsingSystemAccentColor { get; private set; }

		private static readonly CancellationTokenSource s_shutdownCts = new();
		public static CancellationToken ShutdownToken => s_shutdownCts.Token;

		public static bool IsShuttingDown => ShutdownToken.IsCancellationRequested;

		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code executed,
		/// and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Invoked when the application is launched.
		/// </summary>
		/// <param name="args">Details about the launch request and process.</param>
		protected override void OnLaunched(LaunchActivatedEventArgs args)
		{
			Debug.WriteLine("App OnLaunched");

			Preferences.Load();
			LocationManager.Load();

			WeatherDataCache.Initialize();
			WeatherDataCache.CleanupRecords();

			WindowUtils.ApplyAccentColor(); // Before the Window is created
			StartedUsingSystemAccentColor = Preferences.UseSystemAccentColor;

			MainWindow = new MainWindow();

			WindowUtils.SetAppIcon(MainWindow);
			WindowUtils.InitializeTitleBar(MainWindow);
			WindowUtils.ApplyAppTheme(MainWindow);

			// Activate the startup window.
			MainWindow.Activate();

			MainWindow.Closed += (s, e) =>
			{
				Debug.WriteLine("App MainWindow Closed - Shutdown Signal");
				SignalShutdown();
			};
		}

		internal static async Task Restart()
		{
			try
			{
				string exePath = Environment.ProcessPath!;
				Process.Start(new ProcessStartInfo(exePath)
				{
					UseShellExecute = true,
					Arguments = "restart"
				});

				await Task.Delay(200); // Let the new instance spin up
				Application.Current.Exit();
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Manual restart failed: {ex}");
			}
		}

		internal static void SignalShutdown()
		{
			try { s_shutdownCts.Cancel(); }
			catch { }
		}
	}
}
