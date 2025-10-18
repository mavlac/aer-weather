using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Aer.Drawing
{
	public static class Charting
	{
		/// <summary>
		/// Draws a simple demo line chart from top-left to bottom-right.
		/// </summary>
		public static void DrawHomePageChart(CanvasControl sender, CanvasDrawingSession ds)
		{
			float width = (float)sender.ActualWidth;
			float height = (float)sender.ActualHeight;

			Color lineColor;
			Color textColor;

			bool isDarkTheme = sender.ActualTheme switch
			{
				ElementTheme.Dark => true,
				ElementTheme.Light => false,
				_ => Application.Current.RequestedTheme == ApplicationTheme.Dark
			};

			switch (isDarkTheme)
			{
				case false:
					lineColor = Colors.DodgerBlue;
					textColor = Colors.Black;
					break;
				case true:
					lineColor = Colors.CornflowerBlue;
					textColor = Colors.White;
					break;
			}

			// Top-left → bottom-right
			ds.DrawLine(0, 0, width, height, lineColor, 3);

			// Draw some sample value text
			var format = new CanvasTextFormat
			{
				FontFamily = "Segoe UI",
				FontSize = 12
			};
			ds.DrawText("Sample Value", 120, 60, textColor, format);

			// Optional: draw a glyph from Segoe MDL2 Assets
			// Get your custom font from App.xaml
			var weatherFont = (FontFamily)Application.Current.Resources["WeatherIconsFont"];
			var iconFormat = new CanvasTextFormat
			{
				FontFamily = weatherFont.Source,
				FontSize = 24
			};
			ds.DrawText("\uF00D", 50, 80, lineColor, iconFormat);
		}
	}
}
