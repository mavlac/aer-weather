using Aer.Data;
using Microsoft.UI.Xaml;

namespace Aer.Utils
{
	internal static class ThemeUtils
	{
		/// <summary>
		/// Check the OS theme settings
		/// </summary>
		/// <returns>Return if OS setting is Dark or Light</returns>
		public static Theme GetSystemTheme()
		{
			var uiSettings = new Windows.UI.ViewManagement.UISettings();
			var color = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
			
			return
				(color.R < 128 && color.G < 128 && color.B < 128)
					? Theme.Dark
					: Theme.Light;
		}

		/// <summary>
		/// Gets the actually used App theme, based on value in Preferences.
		/// But when Preferences.AppTheme is Default, and application respects OS setting,
		/// it will check the OS theme settings and return that.
		/// </summary>
		public static Theme GetActualAppTheme()
		{
			return
				Preferences.AppTheme switch
				{
					ElementTheme.Dark => Theme.Dark,
					ElementTheme.Light => Theme.Light,
					_ => Application.Current.RequestedTheme == ApplicationTheme.Dark
						 ? Theme.Dark
						 : Theme.Light
				};
		}
	}
}
