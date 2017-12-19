using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using Axis = OxyPlot.Axes.Axis;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using LineSeries = OxyPlot.Series.LineSeries;
using ScatterSeries = OxyPlot.Series.ScatterSeries;

// Only used by the PNG Exporter

namespace PPMErrorCharter
{
    /// <summary>
    /// Generate mass error plots using OxyPlot
    /// </summary>
    public class IdentDataPlotter : DataPlotterBase
    {

        /// <summary>
        /// Bitmap of the graphic created by ErrorHistogramsToPng
        /// </summary>
        /// <remarks>Initially null</remarks>
        public RenderTargetBitmap ErrorHistogramBitmap { get; private set; }

        /// <summary>
        /// Bitmap of the graphic created by ErrorScatterPlotsToPng
        /// </summary>
        /// <remarks>Initially null</remarks>
        public RenderTargetBitmap ErrorScatterPlotBitmap { get; private set; }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="baseOutputFilePath"></param>
        public IdentDataPlotter(ErrorCharterOptions options, string baseOutputFilePath) : base(options, baseOutputFilePath)
        {
            ErrorHistogramBitmap = null;
            ErrorScatterPlotBitmap = null;
        }

        private PlotModel ModelBaseConfig()
        {
            return new PlotModel
            {
                TitlePadding = 0
            };
        }

        public PlotModel ScatterPlot(IReadOnlyCollection<IdentData> data, string xDataField, string yDataField, string title, string xTitle, OxyColor markerColor)
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
                Mapping = item => new ScatterPoint(Convert.ToDouble(typeof(IdentData).GetProperty(xDataField)?.GetValue(item)), Convert.ToDouble(typeof(IdentData).GetProperty(yDataField)?.GetValue(item)))
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
                FilterMaxValue = 20.0
            };

            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = xTitle
            };

            var xAxisCenter = new LinearAxis
            {
                Position = AxisPosition.Top, // To let the other axis be locked to the bottom, and force this one to be horizontal
                PositionAtZeroCrossing = true,
                TickStyle = TickStyle.None, // To change to show a value, need to do axis synchronization
                AxislineStyle = LineStyle.Solid,
                AxislineThickness = 1.0,
                TextColor = OxyColors.Undefined // Force invisible labels
            };

            model.Axes.Add(yAxis);
            model.Axes.Add(xAxis);
            model.Axes.Add(xAxisCenter);
            model.Series.Add(s1);
            return model;
        }

        public PlotModel ScatterPlot(IReadOnlyCollection<IdentData> data, Func<object, ScatterPoint> mapping, string title, string xTitle, OxyColor markerColor)
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
                Mapping = mapping
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
                FilterMaxValue = 20.0
            };

            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = xTitle
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
                TextColor = OxyColors.Undefined // Force invisible labels
            };

            model.Axes.Add(yAxis);
            model.Axes.Add(xAxis);
            model.Axes.Add(xAxisCenter);
            model.Series.Add(s1);
            return model;
        }

        /// <summary>
        /// Generate the mass error scatter plots and save to a PNG file
        /// </summary>
        /// <param name="scanData"></param>
        /// <param name="pngFilePath"></param>
        /// <param name="fixedMzMLFileExists"></param>
        /// <param name="haveScanTimes"></param>
        private bool ErrorScatterPlotsToPng(IReadOnlyCollection<IdentData> scanData, string pngFilePath, bool fixedMzMLFileExists, bool haveScanTimes)
        {
            var width = 512;  // 1024 pixels final width
            var height = 384; // 768 pixels final height
            var resolution = 96; //96

            // Draw the bitmaps onto a new canvas internally
            // Allows us to combine them
            var drawVisual = new DrawingVisual();
            var drawContext = drawVisual.RenderOpen();

            AddScatterPlotData(drawContext, scanData, haveScanTimes, width, height, resolution, false);

            // Only add the fixed data if the data file exists
            if (fixedMzMLFileExists)
            {
                AddScatterPlotData(drawContext, scanData, haveScanTimes, width, height, resolution, true);
            }

            drawContext.Close();

            // Turn the canvas back into an image
            //RenderTargetBitmap image = new RenderTargetBitmap(width * 2, height * 2, resolution, resolution, PixelFormats.Pbgra32);
            // Setting the DPI of the PngExporter to a higher-than-normal resolution, with a larger image size,
            //   then leaving this at standard 96 DPI will give a blown-up image; setting this to the same high DPI results in an odd image, without improving it.

            var image = new RenderTargetBitmap(width * 2, height * 2, resolution, resolution, PixelFormats.Pbgra32);
            image.Render(drawVisual);

            // Turn the image into a png bitmap
            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(image));
            using (Stream stream = File.Create(pngFilePath))
            {
                png.Save(stream);
            }
            ErrorScatterPlotBitmap = image;

            return true;
        }

        private void AddScatterPlotData(
            DrawingContext drawContext,
            IReadOnlyCollection<IdentData> scanData,
            bool haveScanTimes,
            int width,
            int height,
            int resolution,
            bool useRefinedData)
        {
            PlotModel massErrorsVsTimePlot;

            string dataTypeSuffix;
            string ppmErrorDataField;
            int plotOffset;

            if (useRefinedData)
            {
                dataTypeSuffix = "Refined";
                ppmErrorDataField = "PpmErrorRefined";
                plotOffset = height;
            }
            else
            {
                dataTypeSuffix = "Original";
                ppmErrorDataField = "PpmError";
                plotOffset = 0;
            }

            if (haveScanTimes)
            {
                var scanPlotTitle = "Scan Time: " + dataTypeSuffix;
                var scanPlotXAxisLabel = "Scan Time (min)";

                if (useRefinedData)
                {
                    massErrorsVsTimePlot =
                        ScatterPlot(scanData, item => new ScatterPoint(((IdentData)item).ScanTimeSeconds, ((IdentData)item).PpmErrorRefined),
                                    scanPlotTitle, scanPlotXAxisLabel, OxyColors.Blue);
                }
                else
                {
                    massErrorsVsTimePlot =
                        ScatterPlot(scanData, item => new ScatterPoint(((IdentData)item).ScanTimeSeconds, ((IdentData)item).PpmError),
                                    scanPlotTitle, scanPlotXAxisLabel, OxyColors.Blue);
                }

            }
            else
            {
                var scanPlotTitle = "Scan Number: " + dataTypeSuffix;
                var scanPlotXAxisLabel = "Scan Number";

                if (useRefinedData)
                {
                    massErrorsVsTimePlot =
                        ScatterPlot(scanData, item => new ScatterPoint(((IdentData)item).ScanIdInt, ((IdentData)item).PpmErrorRefined),
                                    scanPlotTitle, scanPlotXAxisLabel, OxyColors.Blue);

                }
                else
                {
                    massErrorsVsTimePlot =
                        ScatterPlot(scanData, item => new ScatterPoint(((IdentData)item).ScanIdInt, ((IdentData)item).PpmError),
                                    scanPlotTitle, scanPlotXAxisLabel, OxyColors.Blue);
                }
            }

            var massErrorsVsMzPlot = ScatterPlot(scanData, "CalcMz", ppmErrorDataField, "M/Z: " + dataTypeSuffix, "m/z", OxyColors.Green);
            var massErrorsVsTimeBitmap = PngExporter.ExportToBitmap(massErrorsVsTimePlot, width, height, OxyColors.White, resolution);
            var massErrorsVsMzBitmap = PngExporter.ExportToBitmap(massErrorsVsMzPlot, width, height, OxyColors.White, resolution);

            drawContext.DrawImage(massErrorsVsTimeBitmap, new Rect(0, plotOffset, width, height));
            drawContext.DrawImage(massErrorsVsMzBitmap, new Rect(width, plotOffset, width, height));

            //var fileName = pngFilePath.Substring(0, pngFilePath.IndexOf(".png", StringComparison.OrdinalIgnoreCase));
            //using (var file = new FileStream(fileName + "_OrigScan.svg", FileMode.Create, FileAccess.Write, FileShare.None))
            //{
            //  OxyPlot.Wpf.SvgExporter.Export(OrigScan, file, width, height, true);
            //}

        }

        /// <summary>
        /// Generate a frequency histogram using the specified data field
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dataField"></param>
        /// <param name="title"></param>
        /// <param name="lineColor"></param>
        /// <returns></returns>
        public PlotModel Histogram(IReadOnlyCollection<IdentData> data, string dataField, string title, OxyColor lineColor)
        {
            var frequencies = HistogramFrequencies(data, dataField);

            var model = ModelBaseConfig();
            model.Title = title;

            var yStep = 50.0;
            var xStep = 5.0;
            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineStyle = LineStyle.None,
                Title = "Counts",
                Minimum = 0.0,
                FilterMinValue = 0.0,
                MajorStep = yStep,
                Maximum = 0.0
            };

            var yAxisCenter = new LinearAxis
            {
                Position = AxisPosition.Right,
                PositionAtZeroCrossing = true,
                TickStyle = TickStyle.Crossing, //TickStyle.None,
                Minimum = 0.0,
                FilterMinValue = 0.0,
                AxislineStyle = LineStyle.Solid,
                AxislineThickness = 1.0,
                MajorStep = yStep,
                TextColor = OxyColors.Undefined // Force invisible labels
            };

            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                MajorStep = xStep,
                MajorGridlineStyle = LineStyle.Dash,
                MinorStep = 1,
                MinorGridlineStyle = LineStyle.None,
                Title = "Mass error (PPM)",
                Maximum = 0.0,
                Minimum = 0.0,
                FilterMinValue = -51.0, // Prevent huge, hard to read plots
                FilterMaxValue = 51.0 // Prevent huge, hard to read plots
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

            var s1 = new LineSeries
            {
                //MarkerFill = OxyColors.Undefined, //OxyColors.Black,
                //MarkerSize = 2.0,
                //MarkerType = MarkerType.None, //MarkerType.Circle,
                Color = lineColor,
                ItemsSource = frequencies,
                DataFieldX = "Key",
                DataFieldY = "Value"
            };

            model.Axes.Add(yAxis);
            model.Axes.Add(xAxis);
            model.Axes.Add(yAxisCenter);
            model.Series.Add(s1);
            return model;
        }

        /// <summary>
        /// Generate the mass error histogram plots and save to a PNG file
        /// </summary>
        /// <param name="scanData"></param>
        /// <param name="pngFilePath"></param>
        /// <param name="fixedMzMLFileExists"></param>
        /// <returns></returns>
        private bool ErrorHistogramsToPng(IReadOnlyCollection<IdentData> scanData, string pngFilePath, bool fixedMzMLFileExists)
        {
            // Create both histogram models to allow synchronizing the y-axis
            var origError = Histogram(scanData, "PpmError", "Original", OxyColors.Blue);
            var fixError = Histogram(scanData, "PpmErrorRefined", "Refined", OxyColors.Green);

            // Synchronize the histogram plot areas - x and y axis ranges
            var axes = new List<Axis>();
            axes.AddRange(origError.Axes);

            // Don't include fixed axes if no fixed file was found
            if (fixedMzMLFileExists)
            {
                axes.AddRange(fixError.Axes);
            }
            var yAxes = new List<Axis>();
            var xAxes = new List<Axis>();
            var yMax = 0.0;
            var yStep = 0.0;
            var xMin = 0.0;
            var xMax = 0.0;
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

            var resolution = 96;
            var width = 512;  // 1024 pixels final width
            var height = 512; // 512 pixels final height

            // Draw the bitmaps onto a new canvas internally
            // Allows us to combine them
            var image = new RenderTargetBitmap(width * 2, height, resolution, resolution, PixelFormats.Pbgra32);
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
            var drawVisual = new DrawingVisual();
            var drawContext = drawVisual.RenderOpen();

            // Output the graph models to a context
            var oe = PngExporter.ExportToBitmap(origError, width, height, OxyColors.White);
            drawContext.DrawImage(oe, new Rect(0, 0, width, height));

            // Only add the fixed files if the data file exists
            if (fixedMzMLFileExists)
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
            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(image));
            using (Stream stream = File.Create(pngFilePath))
            {
                png.Save(stream);
            }

            ErrorHistogramBitmap = image;

            return true;
        }

        /// <summary>
        /// Generate the mass error scatter plots and mass error histogram plots, saving as PNG files
        /// </summary>
        /// <param name="scanData"></param>
        /// <param name="fixedMzMLFileExists"></param>
        /// <param name="haveScanTimes"></param>
        /// <returns></returns>
        public override bool GeneratePNGPlots(IReadOnlyCollection<IdentData> scanData, bool fixedMzMLFileExists, bool haveScanTimes)
        {
            var scatterPlotFilePath = BaseOutputFilePath + "_MZRefinery_MassErrors.png";
            var histogramPlotFilePath = BaseOutputFilePath + "_MZRefinery_Histograms.png";

            var scatterPlotSuccess = ErrorScatterPlotsToPng(scanData, scatterPlotFilePath, fixedMzMLFileExists, haveScanTimes);
            if (scatterPlotSuccess)
                Console.WriteLine("Generated " + scatterPlotFilePath);
            else
                Console.WriteLine("Error generating " + scatterPlotFilePath);

            var histogramPlotSuccess = ErrorHistogramsToPng(scanData, histogramPlotFilePath, fixedMzMLFileExists);
            if (histogramPlotSuccess)
                Console.WriteLine("Generated " + histogramPlotFilePath);
            else
                Console.WriteLine("Error generating " + histogramPlotFilePath);

            return scatterPlotSuccess && histogramPlotSuccess;
        }
    }
}
