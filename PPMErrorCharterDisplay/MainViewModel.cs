using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Wpf;
using PPMErrorCharter;

namespace PPMErrorCharterDisplay
{
	public class MainViewModel
	{
		public MainViewModel()
		{
			var datasetPathName = "E:\\Test\\Cyano_GC_07_12_25Aug09_Draco_09-05-02";
			var identFile = datasetPathName + ".mzid";
			var dataFileFixed = datasetPathName + "_FIXED.mzML";
			var scanData = MzIdentMLReader.Read(identFile);
			bool dataFileExists = false;
			if (File.Exists(dataFileFixed))
			{
				MzMLReader.ReadMzMl(dataFileFixed, scanData);
				dataFileExists = true;
			}
			//this.OrigScanId = IdentDataPlotter.ScatterOriginalByScanNum(scanData);
			//this.OrigCalcMz = IdentDataPlotter.ScatterOriginalByCalcMz(scanData);
			//this.FixScanId = IdentDataPlotter.ScatterFixedByScanNum(scanData);
			//this.FixCalcMz = IdentDataPlotter.ScatterFixedByCalcMz(scanData);
			this.OrigPpmErrorHist = IdentDataPlotter.Histogram(scanData, "PpmError", "Original", OxyColors.Blue, 0.5);
			this.FixPpmErrorHist = IdentDataPlotter.Histogram(scanData, "PpmErrorFixed", "Refined", OxyColors.Green, 0.5);

			//int width = 512;  // 1024 pixels final width
			//int height = 384; // 768 pixels final height
			//var OSI = PngExporter.ExportToBitmap(OrigScanId, width, height, OxyColors.White);
			//var OMZ = PngExporter.ExportToBitmap(OrigCalcMz, width, height, OxyColors.White);
			//var FSI = PngExporter.ExportToBitmap(FixScanId, width, height, OxyColors.White);
			//var FMZ = PngExporter.ExportToBitmap(FixCalcMz, width, height, OxyColors.White);
			//
			//// Draw the bitmaps onto a new canvas internally
			//// Allows us to combine them
			//DrawingVisual drawVisual = new DrawingVisual();
			//DrawingContext drawContext = drawVisual.RenderOpen();
			//drawContext.DrawImage(OSI, new Rect(0, 0, width, height));
			//drawContext.DrawImage(OMZ, new Rect(width, 0, width, height));
			//drawContext.DrawImage(FSI, new Rect(0, height, width, height));
			//drawContext.DrawImage(FMZ, new Rect(width, height, width, height));
			//drawContext.Close();
			//
			//// Turn the canvas back into an image
			//RenderTargetBitmap image = new RenderTargetBitmap(width * 2, height * 2, 96, 96, PixelFormats.Pbgra32);
			//image.Render(drawVisual);
			//
			//// Turn the image into a png bitmap
			//PngBitmapEncoder png = new PngBitmapEncoder();
			//png.Frames.Add(BitmapFrame.Create(image));
			//using (Stream stream = File.Create(datasetPathName + "_m_z_calibration.png"))
			//{
			//	png.Save(stream);
			//}
			this.AllVis = IdentDataPlotter.ErrorScatterPlotsToPng(scanData, datasetPathName + "_MZRefinery_MassErrors.png", dataFileExists);
		}

		//public PlotModel OrigScanId { get; private set; }
		//public PlotModel OrigCalcMz { get; private set; }
		//public PlotModel FixScanId { get; private set; }
		//public PlotModel FixCalcMz { get; private set; }
		public PlotModel OrigPpmErrorHist { get; private set; }
		public PlotModel FixPpmErrorHist { get; private set; }
		public BitmapSource AllVis { get; private set; }
	}
}
