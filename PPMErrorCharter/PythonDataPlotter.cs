using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OxyPlot;
using OxyPlot.Series;

namespace PPMErrorCharter
{
    /// <summary>
    /// Generate mass error plots using Python
    /// </summary>
    public class PythonDataPlotter : DataPlotterBase
    {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="baseOutputFilePath"></param>
        public PythonDataPlotter(ErrorCharterOptions options, string baseOutputFilePath) : base(options, baseOutputFilePath)
        {

            DeleteTempFiles = true;

            if (PythonPath == null)
                PythonPath = string.Empty;
        }

        /// <summary>
        /// Generate the mass error scatter plots and save to a PNG file
        /// </summary>
        /// <param name="scanData"></param>
        /// <param name="pngFile"></param>
        /// <param name="fixedMzMLFileExists"></param>
        /// <param name="haveScanTimes"></param>
        public override void ErrorScatterPlotsToPng(List<IdentData> scanData, string pngFile, bool fixedMzMLFileExists, bool haveScanTimes)
        {
            var errorVsTimeFilePath = "MassErrorsVsTime_TmpExportData.txt";

            // Export data for the mass errors vs. Time plot
            using (var writer = new StreamWriter(new FileStream(errorVsTimeFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                var headerColumns = new List<string>
                {
                    "Scan Time (minutes)",
                    "Original: Mass Error (PPM)",
                    "Refined: Mass Error (PPM)"
                };

                writer.WriteLine(string.Join("\t", headerColumns));

                if (haveScanTimes)
                {
                    foreach (var item in from x in scanData orderby x.ScanTimeSeconds select x)
                    {
                        writer.WriteLine("{0}\t{1}\t{2}",
                            PRISM.StringUtilities.DblToString(item.ScanTimeSeconds, 3),
                            PRISM.StringUtilities.DblToString(item.PpmError, 3),
                            PRISM.StringUtilities.DblToString(item.PpmErrorRefined, 3));
                    }

                }
                else
                {
                    foreach (var item in from x in scanData orderby x.ScanIdInt select x)
                    {
                        writer.WriteLine("{0}\t{1}\t{2}",
                            item.ScanIdInt,
                            PRISM.StringUtilities.DblToString(item.PpmError, 3),
                            PRISM.StringUtilities.DblToString(item.PpmErrorRefined, 3));
                    }
                }
            }

            var errorVsMassFilePath = "MassErrorsVsMass_TmpExportData.txt";
            using (var writer = new StreamWriter(new FileStream(errorVsMassFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                var headerColumns = new List<string>
                {
                    "m/z",
                    "Original: Mass Error (PPM)",
                    "Refined: Mass Error (PPM)"
                };

                writer.WriteLine(string.Join("\t", headerColumns));

                foreach (var item in from x in scanData orderby x.CalcMz select x)
                {
                    writer.WriteLine("{0}\t{1}\t{2}",
                        PRISM.StringUtilities.DblToString(item.CalcMz, 4),
                        PRISM.StringUtilities.DblToString(item.PpmError,3),
                        PRISM.StringUtilities.DblToString(item.PpmErrorRefined, 3));
                }
            }

        }

        /// <summary>
        /// Generate the mass error histogram plots and save to a PNG file
        /// </summary>
        /// <param name="scanData"></param>
        /// <param name="pngFile"></param>
        /// <param name="dataFileExists"></param>
        /// <returns></returns>
        public override void ErrorHistogramsToPng(List<IdentData> scanData, string pngFile, bool dataFileExists)
        {
            var massErrorHistogram = HistogramFrequencies(scanData, "PpmError");

            var refinedMassErrorHistogram = HistogramFrequencies(scanData, "PpmErrorRefined");

            var mergedHistogramData = new SortedDictionary<double, MassErrorHistogramResult>();

            foreach (var item in massErrorHistogram)
            {
                mergedHistogramData.Add(item.Key, new MassErrorHistogramResult(item.Value));
            }

            foreach (var item in refinedMassErrorHistogram)
            {
                if (mergedHistogramData.TryGetValue(item.Key, out var binData))
                {
                    binData.BinCountRefined = item.Value;
                }
                else
                {
                    mergedHistogramData.Add(item.Key, new MassErrorHistogramResult(0, item.Value));
                }
            }

            var exportFilePath = "Histograms_TmpExportData.txt";

            // Export data for the mass errors vs. Time plot
            using (var writer = new StreamWriter(new FileStream(exportFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                var headerColumns = new List<string>
                {
                    "m/z",
                    "Original: Mass Error (PPM)",
                    "Refined: Mass Error (PPM)"
                };

                writer.WriteLine(string.Join("\t", headerColumns));

                foreach (var item in mergedHistogramData)
                {
                    writer.WriteLine("{0}\t{1}\t{2}",
                        item.Key,
                        item.Value.BinCountOriginal,
                        item.Value.BinCountRefined);

        /// <summary>
        /// Generate the mass error scatter plots and mass error histogram plots, saving as PNG files
        /// </summary>
        /// <param name="scanData"></param>
        /// <param name="fixedMzMLFileExists"></param>
        /// <param name="haveScanTimes"></param>
        /// <returns></returns>
        public override bool GeneratePNGPlots(IReadOnlyCollection<IdentData> scanData, bool fixedMzMLFileExists, bool haveScanTimes)
        {
            var metadataFilePaths = new MetadataFileNamesType
            {
                BaseOutputFile = new FileInfo(BaseOutputFilePath)
            };

            if (metadataFilePaths.BaseOutputFile.DirectoryName == null)
            {
                OnErrorEvent("Unable to determine the parent directory of the base output file: " + BaseOutputFilePath);
                return false;
            }

            var histogramPlotDataExported = ExportHistogramPlotData(
                scanData, metadataFilePaths.BaseOutputFile, fixedMzMLFileExists,
                out var errorHistogramsExportFileName);

            if (!histogramPlotDataExported)
            {
                return false;
            }

            var scatterPlotDataExported = ExportScatterPlotData(
                scanData, fixedMzMLFileExists, haveScanTimes, metadataFilePaths.BaseOutputFile,
                out var massErrorVsTimeExportFileName,
                out var massErrorVsMassExportFileName);

            if (!scatterPlotDataExported)
            {
                return false;
            }

            metadataFilePaths.ErrorHistogramsExportFileName = errorHistogramsExportFileName;
            metadataFilePaths.MassErrorVsTimeExportFileName = massErrorVsTimeExportFileName;
            metadataFilePaths.MassErrorVsMassExportFileName = massErrorVsMassExportFileName;

            var success = GeneratePlotsWithPython(metadataFilePaths);

            return success;
        }

                }

            }

        }
    }
}