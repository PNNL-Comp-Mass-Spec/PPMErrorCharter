using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PRISM;

namespace PPMErrorCharter
{
    /// <summary>
    /// PPM Error Charter Options
    /// </summary>
    public class ErrorCharterOptions
    {
        // Ignore Spelling: OxyPlot

        private const string PROGRAM_DATE = "November 22, 2022";

        /// <summary>
        /// Constructor
        /// </summary>
        public ErrorCharterOptions()
        {
            BaseOutputFilePath = string.Empty;
            DefaultFixedMzMLFiles = new List<string>();
            HistogramPlotFilePath = string.Empty;
            MassErrorPlotFilePath = string.Empty;

            InputFilePath = string.Empty;
            FixedMzMLFilePath = string.Empty;
            OutputDirectoryPath = string.Empty;

            SpecEValueThreshold = MzIdentMLReader.DEFAULT_SPEC_EVALUE_THRESHOLD;
            PPMErrorHistogramBinSize = 0.5;
            PythonPlotting = false;
        }

        /// <summary>
        /// Input file path
        /// </summary>
        [Option("I", ArgPosition = 1, HelpText = "PSM results file; .mzid or .mzid.gz")]
        public string InputFilePath { get; set; }

        /// <summary>
        /// Spec_EValue threshold
        /// </summary>
        [Option("EValue", "Threshold", ArgPosition = 2, HelpText = "Spec EValue Threshold",
            HelpShowsDefault = true, Min = 0, Max = 10)]
        public double SpecEValueThreshold { get; set; }

        /// <summary>
        /// Fixed .mzML file path (optional)
        /// </summary>
        [Option("F", "Fixed", "MzML",
            HelpText = "Path to the .mzML or .mzML.gz file with updated m/z values (created by MSConvert using the mzRefiner filter). " +
                       "If this switch is not used, the program will try to auto-find this file")]
        public string FixedMzMLFilePath { get; set; }

        /// <summary>
        /// Output directory path (optional)
        /// </summary>
        [Option("O", "Output",
            HelpText = "Path to the directory where plots should be created; by default, plots are created in the same directory as the input file")]
        public string OutputDirectoryPath { get; set; }

        /// <summary>
        /// Full path to the histogram plot file
        /// </summary>
        /// <remarks>If empty, will be defined using BaseOutputFilePath</remarks>
        [Option("HistogramPlot", "HP",
            HelpText = "Histogram plot file path to use; overrides use of -O or -Output")]
        public string HistogramPlotFilePath { get; set; }

        /// <summary>
        /// Full path to the mass errors plot file
        /// </summary>
        /// <remarks>If empty, will be defined using BaseOutputFilePath</remarks>
        [Option("MassErrorPlot", "MEP",
            HelpText = "Mass error plot file path to use; overrides use of -O or -Output")]
        public string MassErrorPlotFilePath { get; set; }

        /// <summary>
        /// Mass error histogram bin size (in ppm)
        /// </summary>
        [Option("PPMBinSize", "Histogram", HelpText = "PPM mass error histogram bin size",
            HelpShowsDefault = true, Min = 0.1, Max = 10)]
        public double PPMErrorHistogramBinSize { get; set; }

        /// <summary>
        /// Generate plots with Python
        /// </summary>
        [Option("Python", "PythonPlot", HelpText = "Generate plots with Python")]
        public bool PythonPlotting { get; set; }

        /// <summary>
        /// Create a text file containing the data behind the histograms
        /// </summary>
        [Option("Debug", "Verbose", "Keep",
            HelpText = "Create a tab-delimited text file with detailed mass error information. " +
                       "When /Debug is enabled, PPMErrorCharter will not delete the _TmpExportData.txt files " +
                       "used to pass data to Python for plotting")]
        public bool SaveMassErrorDetails { get; set; }

        /// <summary>
        /// Base output file path; updated by OutputSetOptions
        /// </summary>
        /// <remarks>Ignored if HistogramPlotFilePath and MassErrorPlotFilePath are defined</remarks>
        public string BaseOutputFilePath { get; private set; }

        /// <summary>
        /// Default _FIXED.mzML file paths; used in a warning message by GeneratePlots
        /// </summary>
        public List<string> DefaultFixedMzMLFiles { get; }

        private bool AutoResolveFixedMzMLFile(string inputFilePath, string baseOutputFilePath, out string fixedMzMLFilePath, out string cacheInfoFileName)
        {
            var inputFile = new FileInfo(inputFilePath);
            var baseOutputFile = new FileInfo(baseOutputFilePath);

            var basePaths = new List<string> { baseOutputFile.FullName };

            if (inputFile.DirectoryName != null)
            {
                basePaths.Add(Path.Combine(inputFile.DirectoryName, baseOutputFile.Name));
            }

            cacheInfoFileName = string.Empty;
            DefaultFixedMzMLFiles.Clear();

            foreach (var basePath in basePaths)
            {
                DefaultFixedMzMLFiles.Add(basePath + ".mzML");

                var suffixesToCheck = new List<string>
                {
                    "_FIXED.mzML",
                    "_FIXED.mzML.gz",
                    ".mzML",
                    ".mzML.gz"
                };

                foreach (var suffix in suffixesToCheck)
                {
                    var candidateFile = new FileInfo(basePath + suffix);
                    if (!candidateFile.Exists)
                        continue;

                    fixedMzMLFilePath = candidateFile.FullName;
                    return true;
                }

                var cachedMzMLFileFound = ResolveCachedMzMLFile(basePath, out var cachedMzMLFile, out cacheInfoFileName);
                if (cachedMzMLFileFound)
                {
                    fixedMzMLFilePath = cachedMzMLFile.FullName;
                    return true;
                }
            }

            fixedMzMLFilePath = string.Empty;
            return false;
        }

        /// <summary>
        /// Get the assembly version and program date
        /// </summary>
        public static string GetAppVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version + " (" + PROGRAM_DATE + ")";

            return version;
        }

        /// <summary>
        /// Determine the base dataset name and combine it with the output directory path
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="outputDirectoryPath"></param>
        public static string GetBaseOutputFilePath(string inputFilePath, string outputDirectoryPath)
        {
            string outFileStub;

            var mzidIndex = inputFilePath.LastIndexOf(".mzid", StringComparison.OrdinalIgnoreCase);
            if (mzidIndex <= 0)
            {
                outFileStub = Path.GetFileNameWithoutExtension(inputFilePath);
            }
            else
            {
                outFileStub = inputFilePath.Substring(0, mzidIndex);
            }

            var suffixIndex = outFileStub.LastIndexOf("_msgfplus", StringComparison.OrdinalIgnoreCase);

            if (suffixIndex > 0)
            {
                outFileStub = outFileStub.Substring(0, suffixIndex);
            }

            if (string.IsNullOrWhiteSpace(outputDirectoryPath))
                return outFileStub;

            return Path.Combine(outputDirectoryPath, Path.GetFileName(outFileStub));
        }

        /// <summary>
        /// Display the current options
        /// If the FixedMzMLFilePath is undefined, tries to auto-resolve it
        /// </summary>
        public void OutputSetOptions()
        {
            Console.WriteLine("PPMErrorCharter, version {0}", GetAppVersion());
            Console.WriteLine();
            Console.WriteLine("Using options:");

            Console.WriteLine(" {0,-23} {1}", "PSM results file:", InputFilePath);

            BaseOutputFilePath = GetBaseOutputFilePath(InputFilePath, OutputDirectoryPath);

            if (string.IsNullOrWhiteSpace(FixedMzMLFilePath))
            {
                var fixedMzMLFileExists = AutoResolveFixedMzMLFile(InputFilePath, BaseOutputFilePath, out var fixedMzMLFilePath, out var cacheInfoFileName);

                if (fixedMzMLFileExists)
                {
                    FixedMzMLFilePath = fixedMzMLFilePath;
                }
                else
                {
                    Console.WriteLine(" {0,-23} {1}", "Fixed .mzML file:", "Undefined; could not find any of these files");
                    foreach (var defaultFile in DefaultFixedMzMLFiles)
                    {
                        Console.WriteLine(" {0,-23} {1}", string.Empty, defaultFile);
                    }
                    Console.WriteLine(" {0,-23} {1}", string.Empty, cacheInfoFileName);

                    Console.WriteLine();
                }
            }

            if (!string.IsNullOrWhiteSpace(FixedMzMLFilePath))
            {
                Console.WriteLine(" {0,-23} {1}", "Fixed .mzML file", FixedMzMLFilePath);
            }

            var baseOutputFile = new FileInfo(BaseOutputFilePath);

            var histogramPlotFile = new FileInfo(baseOutputFile.FullName + "_Histograms.png");
            var scatterPlotFile = new FileInfo(baseOutputFile.FullName + "_MassErrors.png");

            if (string.IsNullOrEmpty(HistogramPlotFilePath))
            {
                Console.WriteLine(" {0,-23} {1}", "Histogram plot:", histogramPlotFile.FullName);
            }
            else
            {
                Console.WriteLine(" {0,-23} {1}", "Histogram plot:", HistogramPlotFilePath);
            }

            if (string.IsNullOrEmpty(MassErrorPlotFilePath))
            {
                Console.WriteLine(" {0,-23} {1}", "Mass Errors plot:", scatterPlotFile.FullName);
            }
            else
            {
                Console.WriteLine(" {0,-23} {1}", "Mass Errors plot:", MassErrorPlotFilePath);
            }

            Console.WriteLine(" {0,-23} {1}", "Spec EValue threshold:", StringUtilities.DblToString(SpecEValueThreshold, 2));

            Console.WriteLine(" {0,-23} {1}", "Histogram bin size:", StringUtilities.DblToString(PPMErrorHistogramBinSize, 2));

            Console.WriteLine(" {0,-23} {1}", "Generating plots with:", PythonPlotting ? "Python" : "OxyPlot");

            Console.WriteLine();
        }

        /// <summary>
        /// Look for the mzML CacheInfo file
        /// If it exists, determine the path of the .mzML.gz file that it points to and verify that the file exists
        /// </summary>
        /// <param name="baseOutputFilePath"></param>
        /// <param name="cachedMzMLFile"></param>
        /// <param name="cacheInfoFileName"></param>
        /// <returns>True if the CacheInfo file was found and the .mzML.gz file was successfully retrieved; otherwise false</returns>
        private bool ResolveCachedMzMLFile(string baseOutputFilePath, out FileInfo cachedMzMLFile, out string cacheInfoFileName)
        {
            cacheInfoFileName = baseOutputFilePath + ".mzML.gz_CacheInfo.txt";

            try
            {
                var cacheInfoFile = new FileInfo(cacheInfoFileName);
                if (!cacheInfoFile.Exists)
                {
                    cachedMzMLFile = null;
                    return false;
                }

                string cachedMzMLFilePath;

                using (var reader = new StreamReader(new FileStream(cacheInfoFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    if (reader.EndOfStream)
                        cachedMzMLFilePath = string.Empty;
                    else
                        cachedMzMLFilePath = reader.ReadLine();
                }

                if (string.IsNullOrWhiteSpace(cachedMzMLFilePath))
                {
                    cachedMzMLFile = null;
                    return false;
                }

                cachedMzMLFile = new FileInfo(cachedMzMLFilePath);
                if (cachedMzMLFile.Exists)
                    return true;

                ConsoleMsgUtils.ShowWarning("Cached .mzML.gz file specified by {0} not found;\n" +
                                            "Cannot use: {1}", cacheInfoFile.Name, cachedMzMLFilePath);
                cachedMzMLFile = null;
                return false;
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowError("Error looking for the fixed mzML file using the CacheInfo file", ex);
                cachedMzMLFile = null;
                return false;
            }
        }

        /// <summary>
        /// Validate the command line arguments
        /// </summary>
        /// <param name="errorMessage"></param>
        public bool ValidateArgs(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(InputFilePath))
            {
                errorMessage = "PSM results file not specified";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}