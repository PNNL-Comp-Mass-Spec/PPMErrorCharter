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
			var datasetPathName = "E:\\Test4\\Cyano_GC_08_17_15Jun09_Draco_09-05-01";
			var identFile = datasetPathName + ".mzid";
			var dataFileFixed = datasetPathName + "_FIXED.mzML";
			var scanData = MzIdentMLReader.Read(identFile);
			bool dataFileExists = false;
			if (File.Exists(dataFileFixed))
			{
				MzMLReader.ReadMzMl(dataFileFixed, scanData);
				dataFileExists = true;
			}

			//this.OrigScanId = IdentDataPlotter.ScatterPlot(scanData, "ScanIdInt", "PpmError", "Scan Number: Original", OxyColors.Blue);
			//this.OrigCalcMz = IdentDataPlotter.ScatterPlot(scanData, "CalcMz", "PpmError", "M/Z: Original", OxyColors.Green);
			//this.FixScanId =  IdentDataPlotter.ScatterPlot(scanData, "ScanIdInt", "PpmErrorFixed", "Scan Number: Refined", OxyColors.Blue);
			//this.FixCalcMz =  IdentDataPlotter.ScatterPlot(scanData, "CalcMz", "PpmErrorFixed", "M/Z: Refined", OxyColors.Green);
			//this.OrigPpmErrorHist = IdentDataPlotter.Histogram(scanData, "PpmError", "Original", OxyColors.Blue, 0.5);
			//this.FixPpmErrorHist = IdentDataPlotter.Histogram(scanData, "PpmErrorFixed", "Refined", OxyColors.Green, 0.5);

			this.AllVis = IdentDataPlotter.ErrorScatterPlotsToPng(scanData, datasetPathName + "_MZRefinery_MassErrors.png", dataFileExists);
			this.ErrHist = IdentDataPlotter.ErrorHistogramsToPng(scanData, datasetPathName + "_MZRefinery_Histograms.png", dataFileExists);
		}

		//public PlotModel OrigScanId { get; private set; }
		//public PlotModel OrigCalcMz { get; private set; }
		//public PlotModel FixScanId { get; private set; }
		//public PlotModel FixCalcMz { get; private set; }
		public PlotModel OrigPpmErrorHist { get; private set; }
		public PlotModel FixPpmErrorHist { get; private set; }
		public BitmapSource AllVis { get; private set; }
		public BitmapSource ErrHist { get; private set; }
	}
}
