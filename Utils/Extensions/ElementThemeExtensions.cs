using Microsoft.UI.Xaml;
using System;

namespace Aer.Utils.Extensions
{
	internal static class ElementThemeExtensions
	{
		public static ElementTheme Opposite(this ElementTheme elementTheme)
		{
			return elementTheme switch
			{
				ElementTheme.Dark => ElementTheme.Light,
				ElementTheme.Light => ElementTheme.Dark,
				_ => ElementTheme.Default
			};
		}
	}
}
