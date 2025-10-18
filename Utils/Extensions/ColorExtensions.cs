using Windows.UI;

namespace Aer.Utils.Extensions
{
	internal static class ColorExtensions
	{
		public static Color WithAlpha(this Color color, byte alpha)
		{
			return Color.FromArgb(alpha, color.R, color.G, color.B);
		}
	}
}
