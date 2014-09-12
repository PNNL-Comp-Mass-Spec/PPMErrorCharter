using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OxyPlot;
using OxyPlot.Wpf; // Only used by the PNG Exporter

namespace PPMErrorCharter
{
	using OxyPlot.Axes;
	using OxyPlot.Series;

	public class IdentDataPlotter
	{
		private static PlotModel ModelBaseConfig()
		{
			return new PlotModel
			{
				TitlePadding = 0,
			};	
		}

		public static PlotModel ScatterPlot(List<IdentData> data, string xDataField, string yDataField, string title, OxyColor markerColor)
		{
			var model = ModelBaseConfig();
			model.Title = title;

			var s1 = new ScatterSeries
			{
				MarkerType = MarkerType.Circle,
				MarkerStrokeThickness = 0,
				MarkerSize = 1.0,
				MarkerFill = markerColor,
				ItemsSource = data,
				DataFieldX = xDataField,
				DataFieldY = yDataField,
			};

			var yAxis = new LinearAxis
			{
				Position = AxisPosition.Left,
				Title = "Mass Error (PPM)",
				MajorGridlineStyle = LineStyle.Solid,
				MajorStep = 10.0,
				MinorStep = 5.0,
				Minimum = -20.0,
				Maximum = 20.0,
				MinimumRange = 40.0,
				FilterMinValue = -20.0,
				FilterMaxValue = 20.0,
			};

			var xAxis = new LinearAxis
			{
				Position = AxisPosition.Bottom,
				//TitlePosition = 0.0,
			};

			model.Axes.Add(yAxis);
			model.Axes.Add(xAxis);
			model.Series.Add(s1);
			return model;
		}

		public static BitmapSource ErrorScatterPlotsToPng(List<IdentData> scanData, string pngFile, bool dataFileExists)
		{
			int width = 512;  // 1024 pixels final width
			int height = 384; // 768 pixels final height
			
			// Draw the bitmaps onto a new canvas internally
			// Allows us to combine them
			DrawingVisual drawVisual = new DrawingVisual();
			DrawingContext drawContext = drawVisual.RenderOpen();
			var OrigScan = ScatterPlot(scanData, "ScanIdInt", "PpmError", "Scan Number: Original", OxyColors.Blue);
			var OrigMz = ScatterPlot(scanData, "CalcMz", "PpmError", "M/Z: Original", OxyColors.Green);
			var OSI = PngExporter.ExportToBitmap(OrigScan, width, height, OxyColors.White);
			var OMZ = PngExporter.ExportToBitmap(OrigMz, width, height, OxyColors.White);
			drawContext.DrawImage(OSI, new Rect(0, 0, width, height));
			drawContext.DrawImage(OMZ, new Rect(width, 0, width, height));
			
			// Only add the fixed files if the data file exists
			if (dataFileExists)
			{
				var FixScan = ScatterPlot(scanData, "ScanIdInt", "PpmErrorFixed", "Scan Number: Refined", OxyColors.Blue);
				var FixMz = ScatterPlot(scanData, "CalcMz", "PpmErrorFixed", "M/Z: Refined", OxyColors.Green);
				var FSI = PngExporter.ExportToBitmap(FixScan, width, height, OxyColors.White);
				var FMZ = PngExporter.ExportToBitmap(FixMz, width, height, OxyColors.White);
				drawContext.DrawImage(FSI, new Rect(0, height, width, height));
				drawContext.DrawImage(FMZ, new Rect(width, height, width, height));
			}

			drawContext.Close();

			// Turn the canvas back into an image
			RenderTargetBitmap image = new RenderTargetBitmap(width * 2, height * 2, 96, 96, PixelFormats.Pbgra32);
			image.Render(drawVisual);

			// Turn the image into a png bitmap
			PngBitmapEncoder png = new PngBitmapEncoder();
			png.Frames.Add(BitmapFrame.Create(image));
			using (Stream stream = File.Create(pngFile))
			{
				png.Save(stream);
			}
			return image;
		}

		/// <summary>
		/// Create the histogram binned data
		/// </summary>
		/// <param name="data"></param>
		/// <param name="dataField"></param>
		/// <param name="binSize"></param>
		/// <returns></returns>
		private static SortedDictionary<double, int> HistogramFrequencies(List<IdentData> data, string dataField, double binSize)
		{
			Dictionary<double, int> counts = new Dictionary<double, int>();
			int roundingDigits = Convert.ToInt32(Math.Ceiling((1.0 / binSize) / 2));

			foreach (var item in data)
			{
				var value = item.GetType().GetProperty(dataField).GetValue(item);
				var valueExpanded = Convert.ToDouble(value) * (1 / binSize);
				var roundedExpanded = Math.Round(valueExpanded);
				var roundedSmall = roundedExpanded / (1 / binSize);
				var final = Math.Round(roundedSmall, roundingDigits);
				if (!counts.ContainsKey(final))
				{
					counts.Add(final, 0);
				}
				counts[final]++;
			}

			// Sort once?
			return new SortedDictionary<double, int>(counts);
		}

		/// <summary>
		/// Generate a frequency histogram using the specified data field
		/// </summary>
		/// <param name="data"></param>
		/// <param name="dataField"></param>
		/// <param name="title"></param>
		/// <param name="lineColor"></param>
		/// <param name="binSize"></param>
		/// <returns></returns>
		public static PlotModel Histogram(List<IdentData> data, string dataField, string title, OxyColor lineColor, double binSize)
		{
			var frequencies = HistogramFrequencies(data, dataField, binSize);

			var model = ModelBaseConfig();
			model.Title = title;

			double yStep = 50.0;
			double xStep = 5.0;
			var yAxis = new LinearAxis()
			{
				Position = AxisPosition.Left,
				MajorGridlineStyle = LineStyle.Dash,
				Title = "Counts",
				Minimum = 0.0,
				FilterMinValue = 0.0,
				MajorStep = yStep,
				Maximum = 0.0,
			};
			var xAxis = new LinearAxis()
			{
				Position = AxisPosition.Bottom,
				MajorStep = xStep,
				MajorGridlineStyle = LineStyle.Dash,
				Title = "Mass error (PPM)",
				Maximum = 0.0,
				Minimum = 0.0,
			};
			foreach (var frequency in frequencies)
			{
				if (frequency.Value > yAxis.Maximum)
				{
					yAxis.Maximum = Math.Ceiling(Convert.ToDouble(frequency.Value) / yStep) * yStep;
				}
				if (frequency.Key < xAxis.Minimum)
				{
					xAxis.Minimum = Math.Floor(Convert.ToDouble(frequency.Key) / xStep) * xStep;
				}
				if (frequency.Key > xAxis.Maximum)
				{
					xAxis.Maximum = Math.Ceiling(Convert.ToDouble(frequency.Key) / xStep) * xStep;
				}
			}

			var s1 = new LineSeries()
			{
				MarkerFill = OxyColors.Black,
				MarkerSize = 2.0,
				MarkerType = MarkerType.Circle,
				Color = lineColor,
				ItemsSource = frequencies,
				DataFieldX = "Key",
				DataFieldY = "Value",
			};
			model.Axes.Add(yAxis);
			model.Axes.Add(xAxis);
			model.Series.Add(s1);
			return model;
		}

		/// <summary>
		/// Output the original and refined ppm error histograms to a single file
		/// </summary>
		/// <param name="scanData"></param>
		/// <param name="pngFile"></param>
		/// <param name="dataFileExists"></param>
		/// <returns></returns>
		public static BitmapSource ErrorHistogramsToPng(List<IdentData> scanData, string pngFile, bool dataFileExists)
		{
			int width = 512;  // 1024 pixels final width
			int height = 512; // 512 pixels final height

			// Draw the bitmaps onto a new canvas internally
			// Allows us to combine them
			DrawingVisual drawVisual = new DrawingVisual();
			DrawingContext drawContext = drawVisual.RenderOpen();
			// Create both histogram models to allow sychronizing the y-axis
			var origError = Histogram(scanData, "PpmError", "Original", OxyColors.Blue, 0.5);
			var fixError = Histogram(scanData, "PpmErrorFixed", "Refined", OxyColors.Green, 0.5);

			// Synchronize the histogram plot areas - x and y axis ranges
			var axes = new List<Axis>();
			axes.AddRange(origError.Axes);
			axes.AddRange(fixError.Axes);
			var yAxes = new List<Axis>();
			var xAxes = new List<Axis>();
			double yMax = 0.0;
			double xMin = 0.0;
			double xMax = 0.0;
			foreach (var axis in axes)
			{
				if (axis.Position == AxisPosition.Left)
				{
					yAxes.Add(axis);
					if (axis.Maximum > yMax)
					{
						yMax = axis.Maximum;
					}
				}
				else if (axis.Position == AxisPosition.Bottom)
				{
					xAxes.Add(axis);
					if (axis.Maximum > xMax)
					{
						xMax = axis.Maximum;
					}
					if (axis.Minimum < xMin)
					{
						xMin = axis.Minimum;
					}
				}
			}
			foreach (var axis in yAxes)
			{
				axis.Maximum = yMax;
			}
			// Make sure the axis is centered...
			if (xMax > Math.Abs(xMin))
			{
				xMin = -xMax;
			}
			else
			{
				xMax = -xMin;
			}
			foreach (var axis in xAxes)
			{
				axis.Maximum = xMax;
				axis.Minimum = xMin;
			}

			// Output the graph models to a context
			var oe = PngExporter.ExportToBitmap(origError, width, height, OxyColors.White);
			drawContext.DrawImage(oe, new Rect(0, 0, width, height));

			// Only add the fixed files if the data file exists
			if (dataFileExists)
			{
				var fe = PngExporter.ExportToBitmap(fixError, width, height, OxyColors.White);
				drawContext.DrawImage(fe, new Rect(width, 0, width, height));
			}

			drawContext.Close();

			// Turn the canvas back into an image
			RenderTargetBitmap image = new RenderTargetBitmap(width * 2, height, 96, 96, PixelFormats.Pbgra32);
			image.Render(drawVisual);

			// Turn the image into a png bitmap
			PngBitmapEncoder png = new PngBitmapEncoder();
			png.Frames.Add(BitmapFrame.Create(image));
			using (Stream stream = File.Create(pngFile))
			{
				png.Save(stream);
			}
			return image;
		}
	}
}
