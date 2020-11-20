using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using PRISM;
using PRISM.Logging;

namespace PPMErrorCharter
{
    public class Program
    {
        // Ignore Spelling: Bryson, mzIdentML, Da, OxyPlot, ExperMZ

        [STAThread]
        private static int Main(string[] args)
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
                                  "from MS-GF+ to evaluate three different calibration methods, then chooses the " +
                                  "optimal transform function to remove systematic bias, typically resulting in a " +
                                  "mass measurement error histogram centered at 0 ppm. MzRefinery is part of the " +
                                  "ProteoWizard package (in the msconvert.exe tool) and it thus can read and write " +
                                  "a wide variety of file formats.",

                    ContactInfo = "Program written by Bryson Gibbons and Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2014" +
                                  Environment.NewLine + Environment.NewLine +
                                  "E-mail: proteomics@pnnl.gov" + Environment.NewLine +
                                  "Website: https://panomics.pnnl.gov/ or https://omics.pnl.gov or https://github.com/PNNL-Comp-Mass-Spec",

                    UsageExamples = {
                        exeName + " SearchResults_msgfplus.mzid.gz",
                        exeName + " SearchResults_msgfplus.mzid.gz 1E-12",
                        exeName + " SearchResults_msgfplus.mzid.gz /Python",
                        exeName + " SearchResults_msgfplus.mzid.gz /Python /Debug",
                        exeName + " SearchResults_msgfplus.mzid.gz /F:C:\\InstrumentFiles\\SearchResults_msgfplus.mzML.gz 1E-12",
                        exeName + " -I:SearchResults_msgfplus.mzid.gz -EValue:1E-13"
                    }
                };

                var parseResults = parser.ParseArgs(args);
                var options = parseResults.ParsedResults;

                if (!parseResults.Success)
                {
                    Thread.Sleep(1500);
                    return -1;
                }

                if (!options.ValidateArgs(out var errorMessage))
                {
                    parser.PrintHelp();

                    Console.WriteLine();
                    OnWarningEvent("Validation error:");
                    OnWarningEvent(errorMessage);

                    Thread.Sleep(1500);
                    return -1;
                }

                // Display the current options
                // If the FixedMzMLFilePath is undefined, OutputSetOptions ties to auto-resolve it
                options.OutputSetOptions();

                // Read the input files, generate histograms, and create the plots
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
                ShowErrorMessage("Error occurred in Program->Main", ex);
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
                OnWarningEvent(string.Format(
                    "Error: \"{0}\" is not an mzIdentML file.\nThe filename should end in .mzid or .mzid.gz",
                    identFilePath));
                return false;
            }

            var identFile = new FileInfo(identFilePath);
            if (!identFile.Exists)
            {
                OnWarningEvent(string.Format(
                    "Error: Data file not found: \"{0}\"", identFilePath));

                if (!Path.IsPathRooted(identFilePath))
                    Console.WriteLine("Full file path: {0}", identFile.FullName);

                return false;
            }

            bool fixedMzMLFileExists;
            if (string.IsNullOrWhiteSpace(options.FixedMzMLFilePath))
            {
                fixedMzMLFileExists = false;
            }
            else
            {
                fixedMzMLFileExists = File.Exists(options.FixedMzMLFilePath);

                if (!fixedMzMLFileExists)
                {
                    OnWarningEvent(string.Format(
                        "Error: Data file not found: \"{0}\"", options.FixedMzMLFilePath));

                    if (!Path.IsPathRooted(options.FixedMzMLFilePath))
                        Console.WriteLine("Full file path: {0}", options.FixedMzMLFilePath);

                    return false;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Creating plots for \"{0}\"", identFile.Name);
            if (!fixedMzMLFileExists)
            {
                if (string.IsNullOrWhiteSpace(options.BaseOutputFilePath) || string.IsNullOrWhiteSpace(options.DefaultFixedMzMLFileName))
                {
                    OnWarningEvent(string.Format(
                        "  Warning: Could not find fixed data file \"{0}[.gz]\"\n  " +
                        "  Output will not include fixed data plots.",
                        Path.GetFileNameWithoutExtension(identFile.Name) + "_FIXED.mzML"));
                }
                else
                {
                    // A message with the expected filename should have already been shown by OutputSetOptions
                    OnWarningEvent("  Output will not include fixed data plots since the fixed .mzML file is not defined");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Loading data from the .mzid file");

            var reader = new MzIdentMLReader(options.SpecEValueThreshold);
            RegisterEvents(reader);

            var psmResults = reader.Read(identFile.FullName);
            bool haveScanTimes;

            if (fixedMzMLFileExists && psmResults.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Loading data from \"{0}\"", PRISM.PathUtils.CompactPathString(options.FixedMzMLFilePath, 80));
                var fixedDataReader = new MzMLReader(options.FixedMzMLFilePath);
                RegisterEvents(fixedDataReader);

                fixedDataReader.ReadSpectraData(psmResults);

                // mzML files are guaranteed to have scan time
                haveScanTimes = true;
                Console.WriteLine();
            }
            else
            {
                haveScanTimes = reader.HaveScanTimes;
            }

            if (psmResults.Count == 0)
            {
                ShowErrorMessage(string.Format(
                    "No PSM results were read from {0}; nothing to plot", identFile.Name));
                return false;
            }

            var stats = new IdentDataStats(psmResults);

            stats.PrintStatsTable();

            var firstResult = psmResults.First();

            Console.WriteLine();
            Console.WriteLine("Using data points with original and refined MassError between {0} and {1} Da",
                              -firstResult.IsotopeErrorFilterWindow, firstResult.IsotopeErrorFilterWindow);

            Console.WriteLine("Using data points with original and refined PpmError between {0} and {1} ppm",
                              -firstResult.PpmErrorFilterWindow, firstResult.PpmErrorFilterWindow);

            var origSize = psmResults.Count;
            var itemsRemoved = 0;
            for (var i = 0; i < psmResults.Count; i++)
            {
                if (psmResults[i].OutOfRange())
                {
                    psmResults.RemoveAt(i);
                    i--; // Step back one value, to hit this same index again
                    itemsRemoved++;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Removed {0:N0} out-of-range items from the original {1:N0} items.", itemsRemoved, origSize);

            DataPlotterBase plotter;

            string baseOutputFilePath;
            if (string.IsNullOrWhiteSpace(options.BaseOutputFilePath))
            {
                if (string.IsNullOrWhiteSpace(options.OutputDirectoryPath))
                {
                    var inputFile = new FileInfo(options.InputFilePath);
                    baseOutputFilePath = ErrorCharterOptions.GetBaseOutputFilePath(options.InputFilePath, inputFile.DirectoryName);
                }
                else
                {
                    baseOutputFilePath = ErrorCharterOptions.GetBaseOutputFilePath(options.InputFilePath, options.OutputDirectoryPath);
                }
            }
            else
            {
                baseOutputFilePath = options.BaseOutputFilePath;
            }

#if DISABLE_OXYPLOT
            var usePythonPlotting = true;
#else
            var usePythonPlotting = options.PythonPlotting;
#endif

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (usePythonPlotting)
            {
                // Make sure that Python exists
                if (!PythonDataPlotter.PythonInstalled)
                {
                    ShowErrorMessage("Could not find the python executable");
                    var debugMsg = "Paths searched:";
                    foreach (var item in PythonDataPlotter.PythonPathsToCheck())
                    {
                        debugMsg += "\n  " + item;
                    }
                    OnDebugEvent(debugMsg);
                    return false;
                }

                var pythonPlotter = new PythonDataPlotter(options, baseOutputFilePath);
                plotter = pythonPlotter;

                if (options.SaveMassErrorDetails)
                {
                    // Do not delete temp files when Debug mode is enabled
                    pythonPlotter.DeleteTempFiles = false;
                }
            }
            else
            {
#if DISABLE_OXYPLOT
                throw new Exception("OxyPlot is disabled; use switch /Python");
#else
                plotter = new IdentDataPlotter(options, baseOutputFilePath);
#endif
            }

            RegisterEvents(plotter);

            var plotsSaved = plotter.GeneratePNGPlots(psmResults, fixedMzMLFileExists, haveScanTimes);

            if (!options.SaveMassErrorDetails)
                return plotsSaved;

            var outFilePath = baseOutputFilePath + "_debug.tsv";

            Console.WriteLine();
            Console.WriteLine("Exporting data to {0}", outFilePath);
            using (var writer = new StreamWriter(new FileStream(outFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                var headerColumns = new List<string>
                {
                    "NativeID",
                    "CalcMZ",
                    // ReSharper disable once StringLiteralTypo
                    "ExperMZ",
                    "RefineMZ",
                    "MassError",
                    "PpmError",
                    "RMassError",
                    "RPpmError",
                    "Charge"
                };

                writer.WriteLine(string.Join("\t", headerColumns));

                foreach (var data in psmResults)
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

                if (!fixedMzMLFileExists)
                {
                    return plotsSaved;
                }
            }

            return plotsSaved;
        }

        /// <summary>Use this method to chain events between classes</summary>
        /// <param name="sourceClass"></param>
        private static void RegisterEvents(IEventNotifier sourceClass)
        {
            sourceClass.DebugEvent += OnDebugEvent;
            sourceClass.StatusEvent += OnStatusEvent;
            sourceClass.ErrorEvent += OnErrorEvent;
            sourceClass.WarningEvent += OnWarningEvent;
            // sourceClass.ProgressUpdate += OnProgressUpdate;
        }

        private static void OnDebugEvent(string message)
        {
            ConsoleMsgUtils.ShowDebug(message);
        }

        private static void OnErrorEvent(string message, Exception ex)
        {
            ConsoleMsgUtils.ShowErrorCustom(message, ex, false);
        }

        private static void OnStatusEvent(string message)
        {
            Console.WriteLine(message);
        }

        private static void OnWarningEvent(string message)
        {
            ConsoleMsgUtils.ShowWarning(message);
        }

        private static void ShowErrorMessage(string message, Exception ex = null)
        {
            ConsoleMsgUtils.ShowError(message, ex);
        }
    }
}
