using System;
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
            InputFilePath = string.Empty;
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
        /// Get the assembly version and program date
        /// </summary>
        /// <returns></returns>
        public static string GetAppVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version + " (" + PROGRAM_DATE + ")";

            return version;
        }

        public void OutputSetOptions()
        {
            Console.WriteLine("PPMErrorCharter, version {0}", GetAppVersion());
            Console.WriteLine();
            Console.WriteLine("Using options:");

            Console.WriteLine(" PSM results file: {0}", InputFilePath);

            Console.WriteLine(" Spec EValue threshold: {0}", StringUtilities.DblToString(SpecEValueThreshold, 2));

            Console.WriteLine(" PPM Error histogram bin size: {0}", StringUtilities.DblToString(PPMErrorHistogramBinSize, 2));

            if (PythonPlotting)
                Console.WriteLine(" Generating plots with Python");
            else
                Console.WriteLine(" Generating plots with OxyPlot");

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