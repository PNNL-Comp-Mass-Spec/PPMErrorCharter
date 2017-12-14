using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using PRISM;

namespace PPMErrorCharter
{
    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                var asmName = typeof(Program).GetTypeInfo().Assembly.GetName();
                var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
                var version = ErrorCharterOptions.GetAppVersion();

                var parser = new CommandLineParser<ErrorCharterOptions>(asmName.Name, version)
                {
                    ProgramInfo = "This program generates plots of the mass measurement errors before and after processing with mzRefinery." + Environment.NewLine + Environment.NewLine +
                                  "mzRefinery is a software tool for correcting systematic mass error biases " +
                                  "in mass spectrometry data files. The software uses confident peptide spectrum matches " +
                                  "from MSGF+ to evaluate three different calibration methods, then chooses the " +
                                  "optimal transform function to remove systematic bias, typically resulting in a " +
                                  "mass measurement error histogram centered at 0 ppm. MzRefinery is part of the " +
                                  "ProteoWizard package (in the msconvert.exe tool) and it thus can read and write " +
                                  "a wide variety of file formats.",

                    ContactInfo = "Program written by Bryson Gibbons and Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2014" +
                                  Environment.NewLine + Environment.NewLine +
                                  "E-mail: proteomics@pnnl.gov" + Environment.NewLine +
                                  "Website: https://panomics.pnnl.gov/ or https://omics.pnl.gov or https://github.com/PNNL-Comp-Mass-Spec",

                    UsageExamples = {
                        exeName + "SearchResults_msgfplus.mzid.gz",
                        exeName + "SearchResults_msgfplus.mzid.gz 1E-12",
                        exeName + "SearchResults_msgfplus.mzid.gz /Python",
                        exeName + "SearchResults_msgfplus.mzid.gz /Python /Debug"
                    }
                };

                var parseResults = parser.ParseArgs(args);
                var options = parseResults.ParsedResults;

                if (!parseResults.Success)
                {
                    Thread.Sleep(1500);
                    return -1;
                }

                if (!options.ValidateArgs())
                {
                    parser.PrintHelp();
                    Thread.Sleep(1500);
                    return -1;
                }

                options.OutputSetOptions();

                var success = GeneratePlots(options);

                if (success)
                {
                    Console.WriteLine("Processing completed successfully");
                    Thread.Sleep(250);
                    return 0;
                }

                Thread.Sleep(1500);
                return -1;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error occurred in Program->Main: " + Environment.NewLine + ex.Message, ex);
                Thread.Sleep(1000);
                return -1;
            }
        }

        private static bool GeneratePlots(ErrorCharterOptions options)
        {

            // Get the file name
            var identFilePath = options.InputFilePath;
            if (!(identFilePath.EndsWith(".mzid", StringComparison.OrdinalIgnoreCase) ||
                  identFilePath.EndsWith(".mzid.gz", StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("Error: \"" + identFilePath + "\" is not an mzIdentML file.");
                return false;
            }

            var identFile = new FileInfo(identFilePath);
            if (!identFile.Exists)
            {
                Console.WriteLine("Error: Data file not found: \"" + identFilePath + "\"");
                if (!Path.IsPathRooted(identFilePath))
                    Console.WriteLine("Full file path: " + identFile.FullName);

                return false;
            }

            var outFileStub = identFile.FullName.Substring(0, identFile.FullName.LastIndexOf(".mzid", StringComparison.OrdinalIgnoreCase));
            if (outFileStub.EndsWith("_msgfplus", StringComparison.OrdinalIgnoreCase))
            {
                outFileStub = outFileStub.Substring(0, outFileStub.LastIndexOf("_msgfplus", StringComparison.OrdinalIgnoreCase));
            }

            var fixedMzMLFile = outFileStub + "_FIXED.mzML";

            var fixedMzMLFileExists = true;
            if (File.Exists(fixedMzMLFile + ".gz"))
            {
                fixedMzMLFile += ".gz";
            }
            else if (!File.Exists(fixedMzMLFile))
            {
                fixedMzMLFileExists = false;
            }

            Console.WriteLine();
            Console.WriteLine("Creating plots for \"" + identFile.Name + "\"");
            if (fixedMzMLFileExists)
            {
                Console.WriteLine("  Using fixed data file \"" + fixedMzMLFile + "\"");
            }
            else
            {
                Console.WriteLine("  Warning: Could not find fixed data file \"" + fixedMzMLFile + "[.gz]\".");
                Console.WriteLine("  Output will not include fixed data graphs.");
            }

            var reader = new MzIdentMLReader(options.SpecEValueThreshold);

            var scanData = reader.Read(identFile.FullName);
            var haveScanTimes = reader.HaveScanTimes;

            if (fixedMzMLFileExists)
            {
                var fixedDataReader = new MzMLReader(fixedMzMLFile);
                fixedDataReader.ReadSpectraData(scanData);
                haveScanTimes = true;
            }

            var stats = new IdentDataStats(scanData);

            stats.PrintStatsTable();

            var origSize = scanData.Count;
            var itemsRemoved = 0;
            for (var i = 0; i < scanData.Count; i++)
            {
                if (scanData[i].OutOfRange())
                {
                    scanData.RemoveAt(i);
                    i--; // Step back one value, to hit this same index again
                    itemsRemoved++;
                }
            }
            Console.WriteLine("Removed " + itemsRemoved + " out-of-range items from the original " + origSize + " items.");

            var scatterPlotFilePath = outFileStub + "_MZRefinery_MassErrors.png";
            var histogramPlotFilePath = outFileStub + "_MZRefinery_Histograms.png";

            DataPlotterBase plotter;

            {
                plotter = new IdentDataPlotter(options);
            }

            plotter.ErrorScatterPlotsToPng(scanData, scatterPlotFilePath, fixedMzMLFileExists, haveScanTimes);
            Console.WriteLine("Generated " + scatterPlotFilePath);

            plotter.ErrorHistogramsToPng(scanData, histogramPlotFilePath, fixedMzMLFileExists);
            Console.WriteLine("Generated " + histogramPlotFilePath);

            if (!options.SaveMassErrorDetails)
                return true;

            var outFilePath = outFileStub + "_debug.tsv";

            Console.WriteLine();
            Console.WriteLine("Exporting data to " + outFilePath);
            using (var writer = new StreamWriter(new FileStream(outFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                var headerColumns = new List<string>
                {
                    "NativeID",
                    "CalcMZ",
                    "ExperMZ",
                    "RefineMZ",
                    "MassError",
                    "PpmError",
                    "RMassError",
                    "RPpmError",
                    "Charge"
                };

                writer.WriteLine(string.Join("\t", headerColumns));

                foreach (var data in scanData)
                {
                    var error = data.MassErrorIsotoped - data.MassErrorRefinedIsotoped;
                    string largeErrorSuffix;
                    if (error < -0.2 || error > 0.2)
                    {
                        largeErrorSuffix = "\tLarge error: " + StringUtilities.DblToString(error, 2);
                    }
                    else
                    {
                        largeErrorSuffix = string.Empty;
                    }

                    writer.WriteLine(data.ToDebugString() + largeErrorSuffix);
                }
            }

            return true;
        }

        private static void ShowErrorMessage(string message, Exception ex = null)
        {
            ConsoleMsgUtils.ShowError(message, ex);
        }

    }
}
