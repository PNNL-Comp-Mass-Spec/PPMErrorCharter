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
        /// <param name="fixedMzMLFileExists"></param>
        /// <param name="haveScanTimes"></param>
        /// <param name="baseOutputFile"></param>
        /// <param name="massErrorVsTimeExportFileName"></param>
        /// <param name="massErrorVsMassExportFileName"></param>
        private bool ExportScatterPlotData(
            IReadOnlyCollection<IdentData> scanData,
            bool fixedMzMLFileExists,
            bool haveScanTimes,
            FileInfo baseOutputFile,
            out string massErrorVsTimeExportFileName,
            out string massErrorVsMassExportFileName)
        {
            if (!PythonInstalled)
            {
                NotifyPythonNotFound("Could not find the python executable");
                massErrorVsTimeExportFileName = string.Empty;
                massErrorVsMassExportFileName = string.Empty;
                return false;
            }

            if (baseOutputFile.DirectoryName == null)
            {
                OnErrorEvent("Unable to determine the parent directory of the base output file: " + baseOutputFile.FullName);
                massErrorVsTimeExportFileName = string.Empty;
                massErrorVsMassExportFileName = string.Empty;
                return false;
            }

            massErrorVsTimeExportFileName = Path.GetFileNameWithoutExtension(baseOutputFile.Name) + "_MassErrorsVsTime" + TMP_FILE_SUFFIX + ".txt";
            var massErrorVsTimeDataExportFile = new FileInfo(Path.Combine(baseOutputFile.DirectoryName, massErrorVsTimeExportFileName));

            // Export data for the mass errors vs. Time plot
            using (var writer = new StreamWriter(new FileStream(massErrorVsTimeDataExportFile.FullName, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                writer.WriteLine("[Title1=Scan Time: Original;Title2=Scan Time: Refined]");
                writer.WriteLine("Autoscale=true;Minimum=0;Maximum=1496298880;StringFormat=#,##0;MinorGridlineThickness=0;MajorStep=1	Autoscale=false;Minimum=-20;Maximum=20;StringFormat=#,##0;MinorGridlineThickness=0;MajorStep=1");

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
                        var ppmErrorRefined = fixedMzMLFileExists ? StringUtilities.DblToString(item.PpmErrorRefined, 3) : string.Empty;

                        writer.WriteLine("{0}\t{1}\t{2}",
                            StringUtilities.DblToString(item.ScanTimeSeconds, 3),
                            StringUtilities.DblToString(item.PpmError, 3),
                            ppmErrorRefined);
                    }

                }
                else
                {
                    foreach (var item in from x in scanData orderby x.ScanIdInt select x)
                    {
                        var ppmErrorRefined = fixedMzMLFileExists ? StringUtilities.DblToString(item.PpmErrorRefined, 3) : string.Empty;

                        writer.WriteLine("{0}\t{1}\t{2}",
                            item.ScanIdInt,
                            StringUtilities.DblToString(item.PpmError, 3),
                            ppmErrorRefined);
                    }
                }
            }

            massErrorVsMassExportFileName = Path.GetFileNameWithoutExtension(baseOutputFile.Name) + "_MassErrorsVsMass" + TMP_FILE_SUFFIX + ".txt";
            var massErrorVsMassDataExportFile = new FileInfo(Path.Combine(baseOutputFile.DirectoryName, massErrorVsMassExportFileName));

            using (var writer = new StreamWriter(new FileStream(massErrorVsMassDataExportFile.FullName, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                writer.WriteLine("[Title1=M/Z: Original;Title2=M/Z: Refined]");
                writer.WriteLine("Autoscale=true;Minimum=0;Maximum=1496298880;StringFormat=#,##0;MinorGridlineThickness=0;MajorStep=1	Autoscale=false;Minimum=-20;Maximum=20;StringFormat=#,##0;MinorGridlineThickness=0;MajorStep=1");

                var headerColumns = new List<string>
                {
                    "m/z",
                    "Original: Mass Error (PPM)",
                    "Refined: Mass Error (PPM)"
                };

                writer.WriteLine(string.Join("\t", headerColumns));

                foreach (var item in from x in scanData orderby x.CalcMz select x)
                {
                    var ppmErrorRefined = fixedMzMLFileExists ? StringUtilities.DblToString(item.PpmErrorRefined, 3) : string.Empty;

                    writer.WriteLine("{0}\t{1}\t{2}",
                        StringUtilities.DblToString(item.CalcMz, 4),
                        StringUtilities.DblToString(item.PpmError, 3),
                        ppmErrorRefined);
                }
            }

            return true;

        }

        /// <summary>
        /// Generate the mass error histogram plots and save to a PNG file
        /// </summary>
        /// <param name="scanData"></param>
        /// <param name="baseOutputFile"></param>
        /// <param name="fixedMzMLFileExists"></param>
        /// <param name="massErrorHistogramsExportFileName"></param>
        /// <returns></returns>
        private bool ExportHistogramPlotData(
            IReadOnlyCollection<IdentData> scanData,
            FileInfo baseOutputFile,
            bool fixedMzMLFileExists,
            out string massErrorHistogramsExportFileName)
        {

            if (baseOutputFile.DirectoryName == null)
            {
                OnErrorEvent("Unable to determine the parent directory of the base output file: " + baseOutputFile.FullName);
                massErrorHistogramsExportFileName = string.Empty;
                return false;
            }

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

            massErrorHistogramsExportFileName = Path.GetFileNameWithoutExtension(baseOutputFile.Name) + "_Histograms" + TMP_FILE_SUFFIX + ".txt";
            var massErrorHistogramsExportFile = new FileInfo(Path.Combine(baseOutputFile.DirectoryName, massErrorHistogramsExportFileName));

            // Export data for the mass errors vs. Time plot
            using (var writer = new StreamWriter(new FileStream(massErrorHistogramsExportFile.FullName, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                writer.WriteLine("[Title1=Original;Title2=Refined]");
                writer.WriteLine("Autoscale=false;Minimum=-50;Maximum=50;StringFormat=#,##0;MinorGridlineThickness=0;MajorStep=1	Autoscale=true;StringFormat=#,##0;MinorGridlineThickness=0;MajorStep=1");

                var headerColumns = new List<string>
                {
                    "Mass error (PPM)",
                    "Original: Counts",
                    "Refined: Counts"
                };

                writer.WriteLine(string.Join("\t", headerColumns));

                foreach (var item in mergedHistogramData)
                {
                    string refinedMassError;
                    if (fixedMzMLFileExists)
                        refinedMassError = item.Value.BinCountRefined.ToString();
                    else
                        refinedMassError = string.Empty;

                    writer.WriteLine("{0}\t{1}\t{2}",
                                     item.Key,
                                     item.Value.BinCountOriginal,
                                     refinedMassError);
                }

            }

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