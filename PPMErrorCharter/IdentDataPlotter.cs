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

		private static ScatterSeries ScatterSeriesBaseConfig()
		{
			return new ScatterSeries
			{
				MarkerType = MarkerType.Circle,
				MarkerStrokeThickness = 0,
				MarkerSize = 1.0,
			};
		}

		private static Axis ScatterYAxisBase()
		{
			return new LinearAxis
			{
				Position = AxisPosition.Left,
				Title = "Mass Error (PPM)",
				MajorGridlineStyle = LineStyle.Solid,
				MajorStep = 5.0,
			};
		}

		private static Axis ScatterXAxisBase()
		{
			return new LinearAxis
			{
				Position = AxisPosition.Bottom,
				//TitlePosition = 0.0,
			};
		}

		private static Axis ScatterXAxisScanNum()
		{
			var axis = ScatterXAxisBase();
			//axis.Title = "scan";
			return axis;
		}

		private static Axis ScatterYAxisScanNum()
		{
			//var axis = ScatterYAxisBase();
			return ScatterYAxisBase();
		}

		private static Axis ScatterXAxisCalcMz()
		{
			var axis = ScatterXAxisBase();
			//axis.Title = "m/z";
			return axis;
		}

		private static Axis ScatterYAxisCalcMz()
		{
			//var axis = ScatterYAxisBase();
			return ScatterYAxisBase();
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
				MajorStep = 5.0,
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

		public static PlotModel ScatterOriginalByScanNum(List<IdentData> data)
		{
			var model = ModelBaseConfig();
			model.Title = "Scan Number: Original";

			var s1 = ScatterSeriesBaseConfig();
			s1.MarkerFill = OxyColors.Blue;
			s1.ItemsSource = data;
			s1.DataFieldX = "ScanIdInt";
			s1.DataFieldY = "PpmError";

			//foreach (var scan in data)
			//{
			//	s1.Points.Add(new ScatterPoint(scan.ScanId, scan.PpmError));
			//}

			model.Series.Add(s1);
			model.Axes.Add(ScatterYAxisScanNum());
			model.Axes.Add(ScatterXAxisScanNum());
			return model;
		}

		public static PlotModel ScatterOriginalByCalcMz(List<IdentData> data)
		{
			var model = ModelBaseConfig();
			model.Title = "M/Z: Original";

			var s1 = ScatterSeriesBaseConfig();
			s1.MarkerFill = OxyColors.Green;
			s1.ItemsSource = data;
			s1.DataFieldX = "CalcMz";
			s1.DataFieldY = "PpmError";

			//foreach (var scan in data)
			//{
			//	s1.Points.Add(new ScatterPoint(scan.CalcMz, scan.PpmError));
			//}

			model.Series.Add(s1);
			model.Axes.Add(ScatterYAxisCalcMz());
			model.Axes.Add(ScatterXAxisCalcMz());
			return model;
		}

		public static PlotModel ScatterFixedByScanNum(List<IdentData> data)
		{
			var model = ModelBaseConfig();
			model.Title = "Scan Number: Refined";

			var s1 = ScatterSeriesBaseConfig();
			s1.MarkerFill = OxyColors.Blue;
			s1.ItemsSource = data;
			s1.DataFieldX = "ScanIdInt";
			s1.DataFieldY = "PpmErrorFixed";

			//foreach (var scan in data)
			//{
			//	s1.Points.Add(new ScatterPoint(scan.ScanId, scan.PpmErrorFixed));
			//}

			model.Series.Add(s1);
			model.Axes.Add(ScatterYAxisScanNum());
			model.Axes.Add(ScatterXAxisScanNum());
			return model;
		}

		public static PlotModel ScatterFixedByCalcMz(List<IdentData> data)
		{
			var model = ModelBaseConfig();
			model.Title = "M/Z: Refined";

			var s1 = ScatterSeriesBaseConfig();
			s1.MarkerFill = OxyColors.Green;
			s1.ItemsSource = data;
			s1.DataFieldX = "CalcMz";
			s1.DataFieldY = "PpmErrorFixed";

			//foreach (var scan in data)
			//{
			//	s1.Points.Add(new ScatterPoint(scan.CalcMz, scan.PpmErrorFixed));
			//}

			model.Series.Add(s1);
			model.Axes.Add(ScatterYAxisCalcMz());
			model.Axes.Add(ScatterXAxisCalcMz());
			return model;
		}

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

		public static PlotModel Histogram(List<IdentData> data, string dataField, string title, OxyColor lineColor, double binSize)
		{
			var model = ModelBaseConfig();
			//model.Title = "Original";
			model.Title = title;

			var yAxis = new LinearAxis()
			{
				Position = AxisPosition.Left,
				MajorGridlineStyle = LineStyle.Dash,
				Title = "Counts",
			};
			var xAxis = new LinearAxis()
			{
				Position = AxisPosition.Bottom,
				MajorStep = 5.0,
				MajorGridlineStyle = LineStyle.Dash,
				Title = "Mass error (PPM)",
				//MinorStep = 1,//0.5,
				//IsAxisVisible = false,
				//LabelField = "PpmError",
				//ItemsSource = data,
			};

			//var frequencies = HistogramFrequencies(data, "PpmError");
			var frequencies = HistogramFrequencies(data, dataField, binSize);

			//for (double i = -30; i <= 30; i += xAxis.MinorStep)
			//for (double i = -10.4; i <= 10.4; i += 0.1)
			////for (int i = -30; i <= 30; i += 5)
			//{
			//	xAxis.Labels.Add(i.ToString());
			//	xAxis.ActualLabels.Add(i.ToString());
			//}
			var s1 = new LineSeries()
			{
				MarkerFill = OxyColors.Black,
				MarkerSize = 2.0,
				MarkerType = MarkerType.Circle,
				//Color = OxyColors.Blue,
				Color = lineColor,
				ItemsSource = frequencies,
				DataFieldX = "Key",
				DataFieldY = "Value",
				//Smooth = true,
			};
			var s2 = new LineSeries()
			{
				Color = OxyColors.Green,
				Smooth = true,
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
			//var OSI = PngExporter.ExportToBitmap(ScatterOriginalByScanNum(scanData), width, height, OxyColors.White);
			//var OMZ = PngExporter.ExportToBitmap(ScatterOriginalByCalcMz(scanData), width, height, OxyColors.White);
			drawContext.DrawImage(OSI, new Rect(0, 0, width, height));
			drawContext.DrawImage(OMZ, new Rect(width, 0, width, height));
			
			// Only add the fixed files if the data file exists
			if (dataFileExists)
			{
				var FixScan = ScatterPlot(scanData, "ScanIdInt", "PpmErrorFixed", "Scan Number: Refined", OxyColors.Blue);
				var FixMz = ScatterPlot(scanData, "CalcMz", "PpmErrorFixed", "M/Z: Refined", OxyColors.Green);
				var FSI = PngExporter.ExportToBitmap(FixScan, width, height, OxyColors.White);
				var FMZ = PngExporter.ExportToBitmap(FixMz, width, height, OxyColors.White);
				//var FSI = PngExporter.ExportToBitmap(ScatterFixedByScanNum(scanData), width, height, OxyColors.White);
				//var FMZ = PngExporter.ExportToBitmap(ScatterFixedByCalcMz(scanData), width, height, OxyColors.White);
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
	}
}
