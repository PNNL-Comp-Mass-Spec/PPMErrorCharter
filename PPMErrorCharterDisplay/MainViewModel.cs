using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
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
            /*/
            Window mainWindow;
            Canvas myParentCanvas;
            Canvas myCanvas1;
            Canvas myCanvas2;
            Canvas myCanvas3;

            // Create the application's main window
            mainWindow = new Window();
            mainWindow.Title = "Canvas Sample";

            // Create the Canvas
            myParentCanvas = new Canvas();
            myParentCanvas.Width = 400;
            myParentCanvas.Height = 400;

            // Define child Canvas elements
            myCanvas1 = new Canvas();
            myCanvas1.Background = Brushes.Red;
            myCanvas1.Height = 100;
            myCanvas1.Width = 100;
            Canvas.SetTop(myCanvas1, 0);
            Canvas.SetLeft(myCanvas1, 0);

            myCanvas2 = new Canvas();
            myCanvas2.Background = Brushes.Green;
            myCanvas2.Height = 100;
            myCanvas2.Width = 100;
            Canvas.SetTop(myCanvas2, 100);
            Canvas.SetLeft(myCanvas2, 100);

            myCanvas3 = new Canvas();
            myCanvas3.Background = Brushes.Blue;
            myCanvas3.Height = 100;
            myCanvas3.Width = 100;
            Canvas.SetTop(myCanvas3, 50);
            Canvas.SetLeft(myCanvas3, 50);

            // Add child elements to the Canvas' Children collection
            myParentCanvas.Children.Add(myCanvas1);
            myParentCanvas.Children.Add(myCanvas2);
            myParentCanvas.Children.Add(myCanvas3);

            // Add the parent Canvas as the Content of the Window Object
            mainWindow.Content = myParentCanvas;
            mainWindow.Show();
            /*/
            var datasetPathName = "E:\\iPRG_2015\\pub_images\\sample4-B_BB5_01_924";
            var identFile = datasetPathName + ".mzid";
            var dataFileFixed = datasetPathName + "_FIXED.mzML";
            var reader = new MzIdentMLReader();
            var scanData = reader.Read(identFile);
            var haveScanTimes = reader.HaveScanTimes;
            var dataFileExists = false;
            if (File.Exists(dataFileFixed))
            {
                var mzML = new MzMLReader(dataFileFixed);
                mzML.ReadSpectraData(scanData);
                dataFileExists = true;
                haveScanTimes = true;
            }

            //this.OrigScanId = IdentDataPlotter.ScatterPlot(scanData, "ScanIdInt", "PpmError", "Scan Number: Original", OxyColors.Blue);
            //this.OrigCalcMz = IdentDataPlotter.ScatterPlot(scanData, "CalcMz", "PpmError", "M/Z: Original", OxyColors.Green);
            //this.FixScanId =  IdentDataPlotter.ScatterPlot(scanData, "ScanIdInt", "PpmErrorFixed", "Scan Number: Refined", OxyColors.Blue);
            //this.FixCalcMz =  IdentDataPlotter.ScatterPlot(scanData, "CalcMz", "PpmErrorFixed", "M/Z: Refined", OxyColors.Green);
            //this.OrigPpmErrorHist = IdentDataPlotter.Histogram(scanData, "PpmError", "Original", OxyColors.Blue, 0.5);
            //this.FixPpmErrorHist = IdentDataPlotter.Histogram(scanData, "PpmErrorFixed", "Refined", OxyColors.Green, 0.5);

            this.AllVis = IdentDataPlotter.ErrorScatterPlotsToPng(scanData, datasetPathName + "_MZRefinery_MassErrors.png", dataFileExists, haveScanTimes);
            this.ErrHist = IdentDataPlotter.ErrorHistogramsToPng(scanData, datasetPathName + "_MZRefinery_Histograms.png", dataFileExists);
            /**/
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
