using Microsoft.UI.Xaml;
using System.Diagnostics;

namespace Aer
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public partial class App : Application
	{
		public static Window? MainWindow { get; private set; }

		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			InitializeComponent();

			Data.LoadLastSavedValues();

			// TODO: Update only if cached data is too old
			Data.UpdateWeatherDataFromNetwork();
		}

		/// <summary>
		/// Invoked when the application is launched.
		/// </summary>
		/// <param name="args">Details about the launch request and process.</param>
		protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
		{
			Debug.WriteLine("App OnLaunched");

			MainWindow = new MainWindow();
			MainWindow.Activate();
		}
	}
}
