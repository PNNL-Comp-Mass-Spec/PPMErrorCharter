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
        private const string PROGRAM_DATE = "July 8, 2019";

        /// <summary>
        /// Constructor
        /// </summary>
        public ErrorCharterOptions()
        {
            BaseOutputFilePath = string.Empty;
            DefaultFixedMzMLFileName = string.Empty;

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
        [Option("F", "Fixed", "FixedMzML", HelpText = "Path to the .mzML or .mzML.gz file with updated m/z values (created by MSConvert using the mzRefiner filter). " +
                                                      "If this switch is not used, the program will try to auto-find this file")]
        public string FixedMzMLFilePath { get; set; }

        /// <summary>
        /// Output directory path (optional)
        /// </summary>
        [Option("O", "Output", HelpText = "Path to the directory where plots should be created; by default, plots are created in the same directory as the input file")]
        public string OutputDirectoryPath { get; set; }

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
        /// Base output file path; updated by OutputSetOptions or
        /// </summary>
        public string BaseOutputFilePath { get; private set; }

        /// <summary>
        /// Default _FIXED.mzML filename; used in a warning message by GeneratePlots
        /// </summary>
        public string DefaultFixedMzMLFileName { get; private set; }

        private bool AutoResolveFixedMzMLFile(string baseOutputFilePath, out string fixedMzMLFilePath, out string cacheInfoFileName)
        {

            DefaultFixedMzMLFileName = baseOutputFilePath + ".mzML";

            var suffixesToCheck = new List<string> {
                "_FIXED.mzML",
                "_FIXED.mzML.gz",
                ".mzML",
                ".mzML.gz"
            };

            foreach (var suffix in suffixesToCheck)
            {
                var candidateFile = new FileInfo(baseOutputFilePath + suffix);
                if (!candidateFile.Exists)
                    continue;

                fixedMzMLFilePath = candidateFile.FullName;
                cacheInfoFileName = string.Empty;
                return true;
            }

            var cachedMzMLFileFound = ResolveCachedMzMLFile(baseOutputFilePath, out var cachedMzMLFile, out cacheInfoFileName);
            if (cachedMzMLFileFound)
            {
                fixedMzMLFilePath = cachedMzMLFile.FullName;
                return true;
            }

            fixedMzMLFilePath = string.Empty;
            return false;
        }

        /// <summary>
        /// Get the assembly version and program date
        /// </summary>
        /// <returns></returns>
        public static string GetAppVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version + " (" + PROGRAM_DATE + ")";

            return version;
        }

        /// <summary>
        /// Determine the base dataset name and prepend it with the output directory path
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <returns></returns>
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

            Console.WriteLine(" PSM results file: {0}", InputFilePath);

            BaseOutputFilePath = GetBaseOutputFilePath(InputFilePath, OutputDirectoryPath);

            if (string.IsNullOrWhiteSpace(FixedMzMLFilePath))
            {
                var fixedMzMLFileExists = AutoResolveFixedMzMLFile(BaseOutputFilePath, out var fixedMzMLFilePath, out var cacheInfoFileName);

                if (fixedMzMLFileExists)
                {
                    FixedMzMLFilePath = fixedMzMLFilePath;
                }
                else
                {
                    Console.WriteLine(" Fixed .mzML file: undefined and could not find \n    {0} or \n    {1}", DefaultFixedMzMLFileName, cacheInfoFileName);
                }
            }

            if (!string.IsNullOrWhiteSpace(FixedMzMLFilePath))
            {
                Console.WriteLine(" Fixed .mzML file: {0}", FixedMzMLFilePath);
            }

            Console.WriteLine(" Spec EValue threshold: {0}", StringUtilities.DblToString(SpecEValueThreshold, 2));

            Console.WriteLine(" PPM Error histogram bin size: {0}", StringUtilities.DblToString(PPMErrorHistogramBinSize, 2));

            if (PythonPlotting)
                Console.WriteLine(" Generating plots with Python");
            else
                Console.WriteLine(" Generating plots with OxyPlot");

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
        /// <returns></returns>
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