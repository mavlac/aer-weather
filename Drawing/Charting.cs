using Aer.Utils;
using Aer.Utils.Extensions;
using Aer.Weather;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Windows.UI;

namespace Aer.Drawing
{
	public static class Charting
	{
		public static void DrawHomePageChart(CanvasControl sender, CanvasDrawingSession ds, List<HourlyForecast> hourly)
		{
			ds.Antialiasing = CanvasAntialiasing.Antialiased;
			ds.TextAntialiasing = CanvasTextAntialiasing.Auto;

			// Responsivity
			float width = (float)sender.ActualWidth;
			float height = (float)sender.ActualHeight;
			const float defaultWidth = 600;
			bool isWide = width > defaultWidth;

			// Helper for working with coordinates with left-bottom 0,0 origin.
			// Running coordinates through this draws them onto canvas that has origin in left-top, which is pain to draw charts in.
			var chart = new ChartSpace(height);

			// Trim data
			const int defaultHours = 61;
			int extraHours = isWide ? (int)((width - defaultWidth) / 10f) : 0; // Each 20 points of width is an extra hour to show
			hourly = hourly.Take(defaultHours + extraHours).ToList();

			// Scan data
			float minTemp = float.PositiveInfinity;
			float maxTemp = float.NegativeInfinity;
			foreach (var hourlyForecast in hourly)
			{
				minTemp = Math.Min(minTemp, (float)hourlyForecast.Temperature);
				maxTemp = Math.Max(maxTemp, (float)hourlyForecast.Temperature);
			}

			// Colors
			bool isDarkTheme;
			Color mainColor, fillColor, freezeFillColor, gridColor, textColor, rainBarColor, snowBarColor;
			switch (sender.ActualTheme switch // Is Dark? If unable to get from FrameworkElement, get from ApplicationTheme
				{
					ElementTheme.Dark => true,
					ElementTheme.Light => false,
					_ => Application.Current.RequestedTheme == ApplicationTheme.Dark
				})
			{
				case false:
					// Light theme
					isDarkTheme = false;
					mainColor = Color.FromArgb(255, 26, 26, 26);
					fillColor = mainColor.WithAlpha(8);
					freezeFillColor = Color.FromArgb(48, 135, 206, 250);
					gridColor = Colors.Gainsboro;
					textColor = Colors.Gray;
					rainBarColor = Colors.LightSkyBlue; // #84C9F3
					snowBarColor = Colors.White;
					break;

				case true:
					// Dark theme
					isDarkTheme = true;
					mainColor = Colors.White;
					fillColor = mainColor.WithAlpha(8);
					freezeFillColor = Color.FromArgb(32, 173, 216, 230);
					gridColor = Color.FromArgb(255, 65, 65, 65);
					textColor = Colors.Gray;
					rainBarColor = Color.FromArgb(255, 19, 130, 197);
					snowBarColor = Color.FromArgb(255, 224, 224, 224);
					break;
			}

			// Visual
			float mainLineStrokeWidth = Preferences.UseThickChartLine ? 1.7f : 1f;
			const float gridStrokeWidth = 1f;
			const float rainSnowBarGutter = 1f;
			const float snowBarHeightMultiplier = 0.2f;
			const float rainBarHeightMultiplier = 0.2f;
			const float snowBarDotRadius = 1.667f;

			// Fonts
			var textFormat = new CanvasTextFormat
			{
				FontFamily = "Segoe UI",
				FontSize = 12
			};
			var textFormatCentered = new CanvasTextFormat
			{
				FontFamily = "Segoe UI",
				FontSize = 12,
				HorizontalAlignment = CanvasHorizontalAlignment.Center,
				VerticalAlignment = CanvasVerticalAlignment.Center
			};
			var weatherFont = (FontFamily)Application.Current.Resources["WeatherIconsFont"];
			var iconFormat = new CanvasTextFormat
			{
				FontFamily = weatherFont.Source,
				FontSize = 13,
				HorizontalAlignment = CanvasHorizontalAlignment.Center,
				VerticalAlignment = CanvasVerticalAlignment.Center
			};

			// Culture
			var culture = CultureInfo.InvariantCulture;

			// Scaling and Range
			float hourWidth = width / (hourly.Count - 1);

			float paddingBottom = Math.Max(100f, height * 0.33f);
			float paddingTop = Math.Max(35f, (height - paddingBottom) * 0.22f);
			//ds.DrawLine(0f, chart.Y(paddingBottom), width, chart.Y(paddingBottom), Colors.Orange, 0.5f);
			//ds.DrawLine(0f, chart.Y(height - paddingTop), width, chart.Y(height - paddingTop), Colors.Orange, 0.5f);
			// total temperature tempRange
			float tempRange = maxTemp - minTemp;
			if (tempRange < 0.1f) tempRange = 0.1f; // avoid division by zero
			// how tall one degree is in pixels
			float degreeHeight = (height - (paddingTop + paddingBottom)) / tempRange;
			// actual position of 0 °C based on data range
			float dataZeroY = (0 - minTemp) * degreeHeight + paddingBottom;
			float zeroDegPositionY = dataZeroY;

			// Main temperature line points
			var temperatureLinePoints = hourly.Select((h, i) =>
			{
				float x = hourWidth * i;
				float y = zeroDegPositionY + (float)h.Temperature * degreeHeight;
				return new Vector2(x, y);
			}).ToList();



			// Drawing

			// Zero degree horizontal line
			// Draw only if not above paddings (can go a bit under the bottom one)
			if (zeroDegPositionY > (paddingBottom - 15) && zeroDegPositionY < height - paddingTop)
			{
				var strokeStyle = new CanvasStrokeStyle();
				strokeStyle.StartCap = strokeStyle.EndCap = CanvasCapStyle.Round;
				strokeStyle.DashStyle = CanvasDashStyle.Dash;
				strokeStyle.CustomDashStyle = [0.5f, 4f];
				ds.DrawLine(0f, chart.Y(zeroDegPositionY), width, chart.Y(zeroDegPositionY), gridColor, mainLineStrokeWidth, strokeStyle);
			}

			// Main temperature spline fill
			var fillBrush = new CanvasSolidColorBrush(ds, fillColor);
			DrawMainTemperatureSpline(ds, temperatureLinePoints, chart, null, 0f, fillBrush);

			// Sub-zero fill
			var freezeFillBrush = new CanvasLinearGradientBrush(ds, freezeFillColor, freezeFillColor.WithAlpha(0))
			{
				StartPoint = new Vector2(0, chart.Y(zeroDegPositionY)), // Gradient brush construction
				EndPoint = new Vector2(0, chart.Y(30f))
			};
			int startSubZeroIndex = -1;
			for (int i = 0; i < hourly.Count; i++)
			{
				bool isLastGraphHour = i == hourly.Count - 1;

				if (startSubZeroIndex == -1 && hourly[i].Temperature < 0)
				{
					startSubZeroIndex = Math.Clamp(i - 1, 0, i); // Start from the previous hour
				}
				else if (startSubZeroIndex != -1 && (hourly[i].Temperature >= 0 || isLastGraphHour))
				{
					// A range to fill
					var subZeroPoints = hourly.Skip(startSubZeroIndex).Take(i - startSubZeroIndex + 1).Select((h, idx) =>
					{
						bool leftEdge = idx is 0;
						bool rightEdge = idx == (i - startSubZeroIndex);
						float x = hourWidth * (startSubZeroIndex + idx);
						if (leftEdge) x += rainSnowBarGutter;
						float y = zeroDegPositionY + (float)h.Temperature * degreeHeight;
						return new Vector2(x, y);
					}).ToList();
					DrawMainTemperatureSpline(ds, subZeroPoints, chart, null, 0f, freezeFillBrush);
					startSubZeroIndex = -1; // Reset
				}
			}

			// Rain and snow bars
			for (int i = 0; i < hourly.Count; i++)
			{
				float rain = (float)hourly[i].Rain;
				float rainRounded = (float)Math.Round(rain, 1);
				float snow = (float)hourly[i].Snowfall;
				float snowRounded = (float)Math.Round(snow, 1);
				bool drawRainBar = rain > float.Epsilon;
				bool drawSnowBar = snow > float.Epsilon;
				float x = hourWidth * i + rainSnowBarGutter;
				float barWidth = hourWidth - rainSnowBarGutter;
				const float bottomLineY = 60f;
				const float minBarHeight = 0.15f;
				float rainBarHeight = (rainRounded + minBarHeight) * (height * rainBarHeightMultiplier);
				float snowBarHeight = (snowRounded + minBarHeight) * (height * snowBarHeightMultiplier);
				bool drawSnowBarDot = !isDarkTheme && drawSnowBar;
				if (drawRainBar && !drawSnowBar)
				{
					ds.FillRectangle(x, chart.Y(bottomLineY), barWidth, -rainBarHeight, rainBarColor);
				}
				else if (drawSnowBar && !drawRainBar)
				{
					ds.FillRectangle(x, chart.Y(bottomLineY), barWidth, -snowBarHeight, snowBarColor);
				}
				else if (drawRainBar && drawSnowBar)
				{
					// The smaller value bar on top of the higher value one
					(float height, Color color) backBar = rain > snow ? (rainBarHeight, rainBarColor) : (snowBarHeight, snowBarColor);
					(float height, Color color) frontBar = rain > snow ? (snowBarHeight, snowBarColor) : (rainBarHeight, rainBarColor);
					// Higher back one
					ds.FillRectangle(x, chart.Y(bottomLineY), barWidth, -backBar.height, backBar.color);
					// And the smaller front one is striped
					DrawStripedBar(ds, x, chart.Y(bottomLineY), barWidth, -frontBar.height, rainBarColor, snowBarColor);
					
					// Snow bar dot only if both bar overlaying and snow is back and significantly higher than rain
					drawSnowBarDot = drawSnowBarDot && snowBarHeight > rainBarHeight + 4;
				}
				
				if (drawSnowBarDot)
				{
					// Light theme snow bars needs to have a dot in order to be visible
					float lineCapY = bottomLineY + snowBarHeight;
					ds.FillCircle(x + barWidth / 2f, chart.Y(lineCapY), snowBarDotRadius, rainBarColor);
				}
			}

			// Legend - Grid
			var labels = new List<(string label, float x, float y)>();
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
						ds.DrawLine(x, chart.Y(y), x, chart.Y(0), gridColor, gridStrokeWidth);

					string label =
						isNewDayMarker
						? culture.DateTimeFormat.GetAbbreviatedDayName(hourly[i].Time.DayOfWeek).ToUpper()
						: hour.ToString();

					bool isLabelOnRightEdge = x > width - 35; // Skip label shortly overlapping right edge
					if (!isLabelOnRightEdge)
						labels.Add((label, x + 8, chart.Y(0 + 22)));
				}
			}
			// Legend - Labels on top of lines
			foreach (var (label, x, y) in labels)
			{
				ds.DrawText(label, x, y, textColor, textFormat);
			}

			// Main temperature spline stroke
			DrawMainTemperatureSpline(ds, temperatureLinePoints, chart, mainColor, mainLineStrokeWidth, null);

			// Temperature readings
			var dayExtremes = new List<DayExtremes>();
			var currentExtremes = new DayExtremes();
			// Daily extremes
			int lastDay = hourly[0].Time.Day;
			for (int i = 0; i < hourly.Count; i++)
			{
				if (hourly[i].Time.Day != lastDay)
				{
					dayExtremes.Add(currentExtremes);
					currentExtremes = new DayExtremes();
					lastDay = hourly[i].Time.Day;
				}
				
				double temperature = hourly[i].Temperature;
				int hour = hourly[i].Time.Hour;
				if (temperature < currentExtremes.DayLow.temperature)
					currentExtremes.DayLow = (temperature, i);
				if (temperature > currentExtremes.DayHigh.temperature)
					currentExtremes.DayHigh = (temperature, i);
			}
			dayExtremes.Add(currentExtremes);

			// Day Low/High labels
			DayExtremes? previousPrintedLow = null;
			DayExtremes? previousPrintedHigh = null;
			foreach (var day in dayExtremes)
			{
				float x, y;
				string label;
				bool isOnLeftEdge, isOnRightEdge, isNextToPrevious;
				bool printLow, printHigh;

				// Low
				x = hourWidth * day.DayLow.chartHour;
				y = zeroDegPositionY + (float)day.DayLow.temperature * degreeHeight;
				label = ((int)Math.Round(TemperatureUtils.GetTemperatureInPreferredUnit(day.DayLow.temperature))).ToString();
				isOnLeftEdge = x < 10;
				isOnRightEdge = x > width - 15;
				isNextToPrevious =
					previousPrintedLow != null && day.DayLow.chartHour - previousPrintedLow.DayLow.chartHour <= 3 ||
					previousPrintedHigh != null && day.DayLow.chartHour - previousPrintedHigh.DayHigh.chartHour <= 3;
				printLow = !isOnRightEdge && !isOnLeftEdge && !isNextToPrevious;
				if (printLow)
					ds.DrawText(label, x, chart.Y(y - 12), mainColor, textFormatCentered);
				
				// High
				x = hourWidth * day.DayHigh.chartHour;
				y = zeroDegPositionY + (float)day.DayHigh.temperature * degreeHeight;
				label = ((int)Math.Round(TemperatureUtils.GetTemperatureInPreferredUnit(day.DayHigh.temperature))).ToString();
				isOnLeftEdge = x < 10;
				isOnRightEdge = x > width - 15;
				isNextToPrevious =
					previousPrintedHigh != null && day.DayHigh.chartHour - previousPrintedHigh.DayHigh.chartHour <= 3 ||
					previousPrintedLow != null && day.DayHigh.chartHour - previousPrintedLow.DayLow.chartHour <= 3;
				printHigh = !isOnRightEdge && !isOnLeftEdge && !isNextToPrevious;
				if (printHigh)
					ds.DrawText(label, x, chart.Y(y + 12), mainColor, textFormatCentered);
				
				if (printLow)
					previousPrintedLow = day;
				if (printHigh)
					previousPrintedHigh = day;
			}

			// Condition icons
			int eachNthHour = isWide ? 3 : 6; // Density
			int startHourlyIndex = 0;
			// When dense, got to start at hours 0,3,6,9,12.. When sparse, got to start at hours 0,6,12,18..
			// Find the start from where to draw first icon
			while (hourly[startHourlyIndex].Time.Hour % eachNthHour != 0) // Look for the first divisible
			{
				startHourlyIndex++;
			}
			for (int i = startHourlyIndex; i < hourly.Count; i += eachNthHour)
			{
				float x = hourWidth * i;
				bool isOnRightEdge = x > width - 35;
				if (!isOnRightEdge)
				{
					var glyph = WeatherIconsUtils.GetWeatherIcon(hourly[i].ConditionCode, hourly[i].IsDaytime);
					ds.DrawText(glyph, x + 15f, chart.Y(40f), textColor, iconFormat);
				}
			}
		}

		public class ChartSpace(float height)
		{
			private readonly float _height = height;
			
			public float Height => _height;
			public float Y(float y) => _height - y;
			public Vector2 XY(float x, float y) => new(x, _height - y);
			public Vector2 XY(Vector2 v) => new(v.X, _height - v.Y);
		}

		/// <summary>
		/// Draws a smooth curve through the given temperatureLinePoints using quadratic Beziers.
		/// Points should be in “data coordinates” (before flipping Y).
		/// </summary>
		public static void DrawMainTemperatureSpline(CanvasDrawingSession ds, List<Vector2> points, ChartSpace chart, Color? lineColor, float lineThickness, ICanvasBrush? fillBrush)
		{
			if (points.Count < 2)
				return;

			// Fill
			if (fillBrush is not null)
			{
				var fillArea = BuildSpline(ds, points, chart);
				
				// Extend to bottom corners
				fillArea.AddLine(new Vector2(chart.XY(points[^1]).X, chart.Y(0)));
				fillArea.AddLine(new Vector2(chart.XY(points[0]).X, chart.Y(0)));
				fillArea.EndFigure(CanvasFigureLoop.Closed);
				
				ds.FillGeometry(CanvasGeometry.CreatePath(fillArea), fillBrush);
			}

			// Spline
			if (lineColor.HasValue)
			{
				var spline = BuildSpline(ds, points, chart);
				
				spline.EndFigure(CanvasFigureLoop.Open);
				var strokeStyle = new CanvasStrokeStyle();
				strokeStyle.StartCap = strokeStyle.EndCap = CanvasCapStyle.Round;
				
				ds.DrawGeometry(CanvasGeometry.CreatePath(spline), lineColor.Value, lineThickness, strokeStyle);
			}



			static CanvasPathBuilder BuildSpline(CanvasDrawingSession ds, List<Vector2> points, ChartSpace chart)
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

		public static void DrawStripedBar(CanvasDrawingSession ds, float x, float y, float width, float height, Color color1, Color color2)
		{
			// Create a small off-screen pattern (tile)
			int patternSize = 4;
			var pattern = new CanvasRenderTarget(ds, patternSize, patternSize, 96);
			using (var patternDs = pattern.CreateDrawingSession())
			{
				// Fill background color
				patternDs.Clear(color1);
				// Draw a stripe
				patternDs.FillRectangle(0, 0, patternSize, patternSize / 2f, color2);
			}

			// Create an image brush using that pattern
			var brush = new CanvasImageBrush(ds, pattern);
			brush.ExtendX = CanvasEdgeBehavior.Wrap;
			brush.ExtendY = CanvasEdgeBehavior.Wrap;
			brush.Interpolation = CanvasImageInterpolation.Linear;
			brush.Transform = Matrix3x2.CreateRotation((float)(-Math.PI / 4.5f));

			// Fill the bar area
			ds.FillRectangle(x, y, width, height, brush);
		}

		private record DayExtremes
		{
			public (double temperature, int chartHour) DayLow = (double.PositiveInfinity, -1);
			public (double temperature, int chartHour) DayHigh = (double.NegativeInfinity, -1);
		}
	}
}
