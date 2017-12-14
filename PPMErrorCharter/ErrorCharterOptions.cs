using System;
using System.Reflection;
using PRISM;

namespace PPMErrorCharter
{
    public class ErrorCharterOptions
    {
        private const string PROGRAM_DATE = "December 13, 2017";

        public ErrorCharterOptions()
        {
            InputFilePath = string.Empty;
            SpecEValueThreshold = MzIdentMLReader.DEFAULT_SPEC_EVALUE_THRESHOLD;
            PPMErrorHistogramBinSize = 0.5;
            PythonPlotting = false;
        }

        [Option("I", ArgPosition = 1, HelpText = "PSM results file; mzid or .mzid.gz")]
        public string InputFilePath { get; set; }

        [Option("EValue", "Threshold", ArgPosition = 2, HelpText = "Spec EValue Threshold",
            HelpShowsDefault = true, Min = 0, Max = 10)]
        public double SpecEValueThreshold { get; set; }

        [Option("PPMBinSize", "Histogram", HelpText = "PPM mass error histogram bin size",
            HelpShowsDefault = true, Min = 0.1, Max = 10)]
        public double PPMErrorHistogramBinSize { get; set; }

        [Option("Python", "PythonPlot", HelpText = "Generate plots with Python")]
        public bool PythonPlotting { get; set; }

        [Option("Debug", "Verbose", HelpText = "Create a tab-delimited text file with detailed mass error information")]
        public bool SaveMassErrorDetails { get; set; }

        public static string GetAppVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version + " (" + PROGRAM_DATE + ")";

            return version;
        }

        public void OutputSetOptions()
        {
            Console.WriteLine("Using options:");

            Console.WriteLine(" PSM results file: {0}", InputFilePath);

            Console.WriteLine(" Spec EValue threshold: {0}", StringUtilities.DblToString(SpecEValueThreshold, 2));

            Console.WriteLine(" PPM Error histogram bin size: {0}", StringUtilities.DblToString(PPMErrorHistogramBinSize, 2));

            if (PythonPlotting)
                Console.WriteLine(" Generating plots with Python");

        }

        public bool ValidateArgs()
        {
            if (string.IsNullOrWhiteSpace(InputFilePath))
            {
                Console.WriteLine("PSM results file not specified");
                return false;
            }

            return true;
        }

    }
}