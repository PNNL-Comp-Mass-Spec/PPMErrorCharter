using System;
using System.Collections.Generic;
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

		public static PlotModel ScatterPlot(List<IdentData> data, string xDataField, string yDataField, string title, string xTitle, OxyColor markerColor)
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
				//DataFieldX = xDataField,
				//DataFieldY = yDataField,
				Mapping = item => new ScatterPoint(Convert.ToDouble(typeof(IdentData).GetProperty(xDataField).GetValue(item)), Convert.ToDouble(typeof(IdentData).GetProperty(yDataField).GetValue(item))),
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
				Title = xTitle,
			};

			var xAxisCenter = new LinearAxis
			{
				Position = AxisPosition.Top, // To let the other axis be locked to the bottom, and force this one to be horizontal
				PositionAtZeroCrossing = true,
				TickStyle = TickStyle.None, // To change to show a value, need to do axis synchronization
				AxislineStyle = LineStyle.Solid,
				AxislineThickness = 1.0,
				TextColor = OxyColors.Undefined, // Force invisible labels
			};

			model.Axes.Add(yAxis);
			model.Axes.Add(xAxis);
			model.Axes.Add(xAxisCenter);
			model.Series.Add(s1);
			return model;
		}

		public static PlotModel ScatterPlot(List<IdentData> data, Func<object, ScatterPoint> mapping, string title, string xTitle, OxyColor markerColor)
		{
			//series1.Mapping = item => new DataPoint(((MyType)item).Time,((MyType)item).Value)
			var model = ModelBaseConfig();
			model.Title = title;

			var s1 = new ScatterSeries
			{
				MarkerType = MarkerType.Circle,
				MarkerStrokeThickness = 0,
				MarkerSize = 1.0,
				MarkerFill = markerColor,
				ItemsSource = data,
				Mapping = mapping,
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
				Title = xTitle,
				//Minimum = 1000,
				//Maximum = 4600,
			};

			var xAxisCenter = new LinearAxis
			{
				Position = AxisPosition.Top, // To let the other axis be locked to the bottom, and force this one to be horizontal
				PositionAtZeroCrossing = true,
				TickStyle = TickStyle.None, // To change to show a value, need to do axis synchronization
				AxislineStyle = LineStyle.Solid,
				AxislineThickness = 1.0,
				TextColor = OxyColors.Undefined, // Force invisible labels
			};

			model.Axes.Add(yAxis);
			model.Axes.Add(xAxis);
			model.Axes.Add(xAxisCenter);
			model.Series.Add(s1);
			return model;
		}

		public static BitmapSource ErrorScatterPlotsToPng(List<IdentData> scanData, string pngFile, bool dataFileExists, bool haveScanTimes)
		{
			int width = 512;  // 1024 pixels final width
			int height = 384; // 768 pixels final height
			int resolution = 96; //96
			
			// Draw the bitmaps onto a new canvas internally
			// Allows us to combine them
			DrawingVisual drawVisual = new DrawingVisual();
			DrawingContext drawContext = drawVisual.RenderOpen();
			PlotModel OrigScan;
			if (haveScanTimes)
			{
				//OrigScan = ScatterPlot(scanData, "ScanTimeSeconds", "PpmError", "Scan Time: Original", OxyColors.Blue);
				OrigScan = ScatterPlot(scanData, item => new ScatterPoint(((IdentData)item).ScanTimeSeconds, ((IdentData)item).PpmError), "Scan Time: Original", "Scan Time (s)", OxyColors.Blue);
			}
			else
			{
				//OrigScan = ScatterPlot(scanData, "ScanIdInt", "PpmError", "Scan Number: Original", OxyColors.Blue);
				OrigScan = ScatterPlot(scanData, item => new ScatterPoint(((IdentData)item).ScanIdInt, ((IdentData)item).PpmError), "Scan Number: Original", "Scan Number", OxyColors.Blue);
			}
			var OrigMz = ScatterPlot(scanData, "CalcMz", "PpmError", "M/Z: Original", "Mass Error (PPM)", OxyColors.Green);
			var OSN = PngExporter.ExportToBitmap(OrigScan, width, height, OxyColors.White, resolution);
			var OMZ = PngExporter.ExportToBitmap(OrigMz, width, height, OxyColors.White, resolution);
			drawContext.DrawImage(OSN, new Rect(0, 0, width, height));
			drawContext.DrawImage(OMZ, new Rect(width, 0, width, height));


			//var fileName = pngFile.Substring(0, pngFile.IndexOf(".png"));
			//using (var file = new FileStream(fileName + "_OrigScan.svg", FileMode.Create, FileAccess.Write, FileShare.None))
			//{
			//	OxyPlot.Wpf.SvgExporter.Export(OrigScan, file, width, height, true);
			//}
			
			// Only add the fixed files if the data file exists
			if (dataFileExists)
			{
				PlotModel FixScan;
				if (haveScanTimes)
				{
					//FixScan = ScatterPlot(scanData, "ScanTimeSeconds", "PpmErrorRefined", "Scan Time: Refined", OxyColors.Blue);
					FixScan = ScatterPlot(scanData, item => new ScatterPoint(((IdentData)item).ScanTimeSeconds, ((IdentData)item).PpmErrorRefined), "Scan Time: Refined", "Scan Time (s)", OxyColors.Blue);
				}
				else
				{
					//FixScan = ScatterPlot(scanData, "ScanIdInt", "PpmErrorRefined", "Scan Number: Refined", OxyColors.Blue);
					FixScan = ScatterPlot(scanData, item => new ScatterPoint(((IdentData)item).ScanIdInt, ((IdentData)item).PpmErrorRefined), "Scan Number: Refined", "Scan Number", OxyColors.Blue);
				}
				var FixMz = ScatterPlot(scanData, "CalcMz", "PpmErrorRefined", "M/Z: Refined", "Mass Error (PPM)", OxyColors.Green);
				var FSN = PngExporter.ExportToBitmap(FixScan, width, height, OxyColors.White, resolution);
				var FMZ = PngExporter.ExportToBitmap(FixMz, width, height, OxyColors.White, resolution);
				drawContext.DrawImage(FSN, new Rect(0, height, width, height));
				drawContext.DrawImage(FMZ, new Rect(width, height, width, height));
				//drawContext.DrawImage(FSN, new Rect(width, 0, width, height));
			}
			
			drawContext.Close();

			// Turn the canvas back into an image
			//RenderTargetBitmap image = new RenderTargetBitmap(width * 2, height * 2, resolution, resolution, PixelFormats.Pbgra32);
            // Setting the DPI of the PngExporter to a higher-than-normal resolution, with a larger image size, 
            //   then leaving this at standard 96 DPI will give a blown-up image; setting this to the same high DPI results in an odd image, without improving it.
			RenderTargetBitmap image = new RenderTargetBitmap(width * 2, height * 2, 96, 96, PixelFormats.Pbgra32);
			//RenderTargetBitmap image = new RenderTargetBitmap(width * 2, height, resolution, resolution, PixelFormats.Pbgra32);
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

			var reflectItem = typeof(IdentData).GetProperty(dataField);

			foreach (var item in data)
			{
				//var value = item.GetType().GetProperty(dataField).GetValue(item);
				var value = reflectItem.GetValue(item);
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
			var yAxisCenter = new LinearAxis()
			{
				Position = AxisPosition.Right,
				PositionAtZeroCrossing = true,
				TickStyle = TickStyle.Crossing, //TickStyle.None,
				Minimum = 0.0,
				FilterMinValue = 0.0,
				AxislineStyle = LineStyle.Solid,
				AxislineThickness = 1.0,
				MajorStep = yStep,
				TextColor = OxyColors.Undefined, // Force invisible labels
			};
			var xAxis = new LinearAxis()
			{
				Position = AxisPosition.Bottom,
				MajorStep = xStep,
				MajorGridlineStyle = LineStyle.Dash,
				Title = "Mass error (PPM)",
				Maximum = 0.0,
				Minimum = 0.0,
				FilterMinValue = -51.0, // Prevent huge, hard to read plots
				FilterMaxValue = 51.0, // Prevent huge, hard to read plots
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
			if (yAxis.Maximum > 500)
			{
				yAxis.MajorStep = Math.Floor((yAxis.Maximum + 499) / 1000) * 100;
				yAxis.Maximum = Math.Ceiling(yAxis.Maximum / yAxis.MajorStep) * yAxis.MajorStep;
			}

			var s1 = new LineSeries()
			{
				//MarkerFill = OxyColors.Undefined, //OxyColors.Black,
				//MarkerSize = 2.0,
				//MarkerType = MarkerType.None, //MarkerType.Circle,
				Color = lineColor,
				ItemsSource = frequencies,
				DataFieldX = "Key",
				DataFieldY = "Value",
			};
			model.Axes.Add(yAxis);
			model.Axes.Add(xAxis);
			model.Axes.Add(yAxisCenter);
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
			// Create both histogram models to allow synchronizing the y-axis
			var origError = Histogram(scanData, "PpmError", "Original", OxyColors.Blue, 0.5);
			var fixError = Histogram(scanData, "PpmErrorRefined", "Refined", OxyColors.Green, 0.5);

			// Synchronize the histogram plot areas - x and y axis ranges
			var axes = new List<Axis>();
			axes.AddRange(origError.Axes);

			// Don't include fixed axes if no fixed file was found
			if (dataFileExists)
			{
				axes.AddRange(fixError.Axes);
			}
			var yAxes = new List<Axis>();
			var xAxes = new List<Axis>();
			double yMax = 0.0;
			double yStep = 0.0;
			double xMin = 0.0;
			double xMax = 0.0;
			foreach (var axis in axes)
			{
				if (axis.IsVertical())
				{
					yAxes.Add(axis);
					if (axis.Maximum > yMax)
					{
						yMax = axis.Maximum;
						yStep = axis.MajorStep;
					}
				}
				else if (axis.IsHorizontal())
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
			if (yMax > 500)
			{
				yStep = Math.Floor((yMax + 499) / 1000) * 100;
				yMax = Math.Ceiling(yMax / yStep) * yStep;
			}

			foreach (var axis in yAxes)
			{
				axis.Maximum = yMax;
				axis.MajorStep = yStep;
				//axis.MinimumRange = yMax;
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

			int resolution = 96;
			int width = 512;  // 1024 pixels final width
			int height = 512; // 512 pixels final height

			// Draw the bitmaps onto a new canvas internally
			// Allows us to combine them
			RenderTargetBitmap image = new RenderTargetBitmap(width * 2, height, resolution, resolution, PixelFormats.Pbgra32);
			/*/
			//RenderTargetBitmap image = new RenderTargetBitmap(4096, 4096, 256, 256, PixelFormats.Pbgra32);
			IPlotModel origModel = origError;
			IPlotModel fixModel = fixError;
			var fullCanvas = new Canvas
			{
				Width = width * 2,
				Height = height,
				Background = OxyColors.White.ToBrush(),
			};
			var origCanvas = new Canvas
			{
				Width = width,
				Height = height,
				Background = OxyColors.White.ToBrush(),
			};
			var fixCanvas = new Canvas
			{
				Width = width,
				Height = height,
				Background = OxyColors.White.ToBrush(),
			};
			fullCanvas.Measure(new Size(fullCanvas.Width, fullCanvas.Height));
			origCanvas.Measure(new Size(origCanvas.Width, origCanvas.Height));
			fixCanvas.Measure(new Size(fixCanvas.Width, fixCanvas.Height));
			fullCanvas.Arrange(new Rect(0, 0, fullCanvas.Width, fullCanvas.Height));
			origCanvas.Arrange(new Rect(0, 0, origCanvas.Width, origCanvas.Height));
			fixCanvas.Arrange(new Rect(0, 0, fixCanvas.Width, fixCanvas.Height));

			var rcOrig = new ShapesRenderContext(origCanvas)
			{
				RendersToScreen = false,
				TextFormattingMode = TextFormattingMode.Ideal,
			};
			var rcFix = new ShapesRenderContext(fixCanvas)
			{
				RendersToScreen = false,
				TextFormattingMode = TextFormattingMode.Ideal,
			};
			//((IPlotModel)origError).Update(true);
			//((IPlotModel)fixError).Update(true);
			//((IPlotModel)origError).Render(rcOrig, origCanvas.Width, origCanvas.Height);
			//((IPlotModel)fixError).Render(rcOrig, fixCanvas.Width, fixCanvas.Height);
			origModel.Update(true);
			fixModel.Update(true);
			origModel.Render(rcOrig, origCanvas.Width, origCanvas.Height);
			fixModel.Render(rcFix, fixCanvas.Width, fixCanvas.Height);

			origCanvas.UpdateLayout();
			fixCanvas.UpdateLayout();

			Canvas.SetTop(origCanvas, 0);
			Canvas.SetLeft(origCanvas, 0);
			fullCanvas.Children.Add(origCanvas);

			Canvas.SetTop(fixCanvas, 0);
			Canvas.SetLeft(fixCanvas, width);
			fullCanvas.Children.Add(fixCanvas);

			fullCanvas.UpdateLayout();

			image.Render(fullCanvas);
			/*/
			DrawingVisual drawVisual = new DrawingVisual();
			DrawingContext drawContext = drawVisual.RenderOpen();

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
			image.Render(drawVisual);
			
			/**/

			// Turn the canvas back into an image
			//RenderTargetBitmap image = new RenderTargetBitmap(width * 2 * (resolution / 96), height * (resolution / 96), resolution, resolution, PixelFormats.Pbgra32);
			//RenderTargetBitmap image = new RenderTargetBitmap(width * 2, height, resolution, resolution, PixelFormats.Pbgra32);
			//image.Render(drawVisual);

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
