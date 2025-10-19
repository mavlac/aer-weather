using Aer.Utils;
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
using System.Linq;
using System.Numerics;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Aer.Drawing
{
	public static class Charting
	{
		public static void DrawHomePageChart(CanvasControl sender, CanvasDrawingSession ds, List<HourlyForecast> hourly)
		{
			ds.Antialiasing = CanvasAntialiasing.Antialiased;
			ds.TextAntialiasing = CanvasTextAntialiasing.Auto;

			float width = (float)sender.ActualWidth;
			float height = (float)sender.ActualHeight;

			// Helper for working with coordinates with left-bottom 0,0 origin.
			// Running coordinates through this draws them onto canvas that has origin in left-top, which is pain to draw charts in.
			var chart = new ChartSpace(height);

			// Trim data
			hourly = hourly.Take(61).ToList();

			// Colors
			Color mainColor, fillColor, lineColor, textColor;
			switch (sender.ActualTheme switch // Is Dark? If unable to get from FrameworkElement, get from ApplicationTheme
				{
					ElementTheme.Dark => true,
					ElementTheme.Light => false,
					_ => Application.Current.RequestedTheme == ApplicationTheme.Dark
				})
			{
				case false:
					// Light theme
					mainColor = Colors.Black;
					fillColor = mainColor.WithAlpha(8);
					lineColor = Color.FromArgb(255, 229, 229, 229);
					textColor = Colors.Gray;
					break;
				case true:
					// Dark theme
					mainColor = Colors.White;
					fillColor = mainColor.WithAlpha(8);
					lineColor = Colors.Gray;
					textColor = Colors.Gray;
					break;
			}

			// Fonts
			var textFormat = new CanvasTextFormat
			{
				FontFamily = "Segoe UI",
				FontSize = 12
			};
			var weatherFont = (FontFamily)Application.Current.Resources["WeatherIconsFont"];
			var iconFormat = new CanvasTextFormat
			{
				FontFamily = weatherFont.Source,
				FontSize = 13
			};


			// Constants
			float hourWidth = width / (hourly.Count - 1);
			float zeroDegPositionY = height / 3f; // TODO: Will be floating based on temperature range
			float degreeHeight = height / 30f;

			// Responsivity
			bool isWide = width > 600;


			// Drawing

			// Zero degrees horizontal line
			var strokeStyle = new CanvasStrokeStyle();
			strokeStyle.DashStyle = CanvasDashStyle.Dash;
			strokeStyle.CustomDashStyle = [1f, 5f];
			ds.DrawLine(0f, chart.Y(zeroDegPositionY), width, chart.Y(zeroDegPositionY), mainColor.WithAlpha(32), 1f, strokeStyle);

			// Time markers - vertical lines
			for (int i = 0; i < hourly.Count; i++)
			{
				int hour = hourly[i].Time.Hour;
				bool isNewDayMarker = hour is 0;
				bool isTimeMarker = isWide ? hour is 6 or 12 or 18 : hour is 12;
				float x = hourWidth * i;
				float y = isNewDayMarker ? zeroDegPositionY + (float)hourly[i].Temperature * degreeHeight : 25;
				if (isNewDayMarker || isTimeMarker)
				{
					bool isLineOnEdge = x < 10 || x > width - 10; // Skip vertical lines that would be on edge of chart, looks bad
					if (!isLineOnEdge)
						ds.DrawLine(x, chart.Y(y), x, chart.Y(0), lineColor, 1f);
					
					string label = isNewDayMarker ? hourly[i].Time.DayOfWeek.ToString() : hour.ToString();

					bool isLabelOnRightEdge = x > width - 30; // Skip label overlapping right edge
					if (!isLabelOnRightEdge)
						ds.DrawText(label, x + 8, chart.Y(0 + 22), textColor, textFormat);
				}
			}

			// Temperature spline
			var temperatureLinePoints = hourly.Select((h, i) =>
			{
				float x = hourWidth * i;
				float y = zeroDegPositionY + (float)h.Temperature * degreeHeight;
				return new Vector2(x, y);
			}).ToList();
			DrawSmoothLine(ds, temperatureLinePoints, chart, mainColor, thickness: 1.6f, fillColor);

			// Condition icons
			int eachNthHour = isWide ? 3 : 5;
			for (int i = 0; i < hourly.Count; i += eachNthHour)
			{
				float x = hourWidth * i;
				var glyph = WeatherIconsUtils.GetWeatherIcon(hourly[i].ConditionCode, hourly[i].IsDaytime);
				ds.DrawText(glyph, x + 8.125f, chart.Y(52f), textColor, iconFormat);
			}
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
		public static void DrawSmoothLine(CanvasDrawingSession ds, List<Vector2> points, ChartSpace chart, Color lineColor, float thickness = 2f, Color? fillColor = null)
		{
			if (points.Count < 2)
				return;

			var spline = BuildSpline(ds, points, chart);

			// If fill is requested, copy path and extend to bottom corners
			if (fillColor.HasValue)
			{
				var fillArea = BuildSpline(ds, points, chart);

				// Extend to bottom corners
				fillArea.AddLine(new Vector2(chart.XY(points[^1]).X, chart.Y(0)));
				fillArea.AddLine(new Vector2(chart.XY(points[0]).X, chart.Y(0)));
				fillArea.EndFigure(CanvasFigureLoop.Closed);

				ds.FillGeometry(CanvasGeometry.CreatePath(fillArea), fillColor.Value);
			}

			// Draw the line on top
			spline.EndFigure(CanvasFigureLoop.Open);
			ds.DrawGeometry(CanvasGeometry.CreatePath(spline), lineColor, thickness);



			CanvasPathBuilder BuildSpline(CanvasDrawingSession ds, List<Vector2> points, ChartSpace chart)
			{
				var builder = new CanvasPathBuilder(ds);
				builder.BeginFigure(chart.XY(points[0]));

				for (int i = 0; i < points.Count - 1; i++)
				{
					var p0 = chart.XY(points[i]);
					var p1 = chart.XY(points[i + 1]);
					var mid = (p0 + p1) / 2;
					builder.AddQuadraticBezier(p0, mid);
				}

				builder.AddLine(chart.XY(points[^1]));
				return builder;
			}
		}
	}
}
