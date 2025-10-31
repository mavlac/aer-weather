using Microsoft.UI.Xaml;

internal static class ThemeUtils
{
	/// <summary>
	/// Check the OS theme settings
	/// </summary>
	/// <returns>Return if OS setting is Dark or Light</returns>
	public static ElementTheme GetSystemTheme()
	{
		var uiSettings = new Windows.UI.ViewManagement.UISettings();
		var color = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
		return (color.R < 128 && color.G < 128 && color.B < 128)
			? ElementTheme.Dark
			: ElementTheme.Light;
	}
}
