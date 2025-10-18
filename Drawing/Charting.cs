using Aer.Utils.Extensions;
using Aer.Weather;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Aer.Drawing
{
	public static class Charting
	{
		/// <summary>
		/// Draws a simple demo line chart from top-left to bottom-right.
		/// </summary>
		public static void DrawHomePageChart(CanvasControl sender, CanvasDrawingSession ds, List<HourlyForecast> hourly)
		{
			ds.Antialiasing = CanvasAntialiasing.Antialiased;
			ds.TextAntialiasing = CanvasTextAntialiasing.Auto;

			float width = (float)sender.ActualWidth;
			float height = (float)sender.ActualHeight;

			// Helper for working with coordinates with left-bottom 0,0 origin.
			// Running coordinates through this draws them onto canvas that has origin in left-top,
			// which is pain to draw charts in.
			var chart = new ChartSpace(height);

			// Trim data
			hourly = hourly.Take(61).ToList();

			// Colors
			Color lineColor;
			Color textColor;
			Color dimColor;
			bool isDarkTheme = sender.ActualTheme switch
			{
				ElementTheme.Dark => true,
				ElementTheme.Light => false,
				_ => Application.Current.RequestedTheme == ApplicationTheme.Dark
			};
			switch (isDarkTheme)
			{
				case false:
					lineColor = Colors.Black;
					textColor = Colors.Black;
					dimColor = Colors.Gray;
					break;
				case true:
					lineColor = Colors.White;
					textColor = Colors.White;
					dimColor = Colors.Gray;
					break;
			}

			// Constants
			float hourWidth = width / (hourly.Count - 1);
			float zeroDegPositionY = height / 3f; // TODO: Will be floating based on temperature range
			float degreeHeight = height / 30f;

			// Drawing

			// Temperature spline
			var temperatureLinePoints = hourly.Select((h, i) =>
			{
				float x = hourWidth * i;
				float y = zeroDegPositionY + (float)h.Temperature * degreeHeight;
				return new Vector2(x, y);
			}).ToList();
			Debug.WriteLine($"width={width}, lastPointX={temperatureLinePoints[^1].X}");
			DrawSmoothLine(ds, temperatureLinePoints, chart, lineColor, thickness: 1.75f);

			// Zero deg line
			ds.DrawLine(0f, chart.Y(zeroDegPositionY), width, chart.Y(zeroDegPositionY), dimColor, 1f);

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

		private static Color GetAccentColor()
		{
			var uiSettings = new UISettings();
			return uiSettings.GetColorValue(UIColorType.Accent);
		}

		public class ChartSpace(float height)
		{
			private readonly float _height = height;
			
			public float Y(float y) => _height - y;
			public Vector2 XY(float x, float y) => new(x, _height - y);
			public Vector2 XY(Vector2 v) => new(v.X, _height - v.Y);
		}

		/// <summary>
		/// Draws a smooth curve through the given temperatureLinePoints using quadratic Beziers.
		/// Points should be in “data coordinates” (before flipping Y).
		/// </summary>
		public static void DrawSmoothLine(CanvasDrawingSession ds, List<Vector2> points, ChartSpace chart, Color color, float thickness = 2f)
		{
			if (points.Count < 2)
				return;

			var spline = new CanvasPathBuilder(ds);
			spline.BeginFigure(chart.XY(points[0]));

			for (int i = 0; i < points.Count - 1; i++)
			{
				var p0 = chart.XY(points[i]);
				var p1 = chart.XY(points[i + 1]);
				var mid = (p0 + p1) / 2;
				spline.AddQuadraticBezier(p0, mid);
			}
			spline.AddLine(chart.XY(points[^1])); // Connect last midpoint to final point

			spline.EndFigure(CanvasFigureLoop.Open);
			var geometry = CanvasGeometry.CreatePath(spline);
			ds.DrawGeometry(geometry, color, thickness);
		}
	}
}
