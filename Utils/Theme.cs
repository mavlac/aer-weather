using Microsoft.UI.Xaml;

namespace Aer.Utils
{
	public enum Theme
	{
		Light,
		Dark
	}

	public static class ThemeExtensions
	{
		public static ElementTheme ToElementTheme(this Theme theme)
		{
			return theme switch
			{
				Theme.Light => ElementTheme.Light,
				Theme.Dark  => ElementTheme.Dark,
				_ => throw new System.NotImplementedException()
			};
		}
	}
}
