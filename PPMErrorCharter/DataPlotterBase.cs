﻿using System;
using System.Collections.Generic;
using System.IO;
using PRISM;

namespace PPMErrorCharter
{
    /// <summary>
    /// Base class for mass error plot generation
    /// </summary>
    public abstract class DataPlotterBase : EventNotifier
    {
        /// <summary>
        /// Base output file path
        /// </summary>
        public string BaseOutputFilePath { get; }

        /// <summary>
        /// Plotting options
        /// </summary>
        public ErrorCharterOptions Options { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="baseOutputFilePath"></param>
        protected DataPlotterBase(ErrorCharterOptions options, string baseOutputFilePath)
        {
            BaseOutputFilePath = baseOutputFilePath;
            Options = options;
        }

        public abstract bool GeneratePNGPlots(IReadOnlyCollection<IdentData> scanData, bool fixedMzMLFileExists, bool haveScanTimes);

        /// <summary>
        /// Create the histogram binned data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dataField"></param>
        protected SortedDictionary<double, int> HistogramFrequencies(IReadOnlyCollection<IdentData> data, string dataField)
        {
            var binSize = Options.PPMErrorHistogramBinSize;
            if (binSize < 0.1)
                binSize = 0.1;

            var counts = new Dictionary<double, int>();
            var roundingDigits = Convert.ToInt32(Math.Ceiling(1 / binSize / 2));

            var reflectItem = typeof(IdentData).GetProperty(dataField);
            if (reflectItem == null)
                return new SortedDictionary<double, int>(counts);

            foreach (var item in data)
            {
                //var value = item.GetType().GetProperty(dataField).GetValue(item);
                var value = reflectItem.GetValue(item);
                var valueExpanded = Convert.ToDouble(value) * (1 / binSize);
                var roundedExpanded = Math.Round(valueExpanded);
                var roundedSmall = roundedExpanded / (1 / binSize);
                var final = Math.Round(roundedSmall, roundingDigits);
                if (!counts.ContainsKey(final))
                {
                    counts.Add(final, 0);
                }
                counts[final]++;
            }

            // Sort once?
            return new SortedDictionary<double, int>(counts);
        }

        public bool ValidateOutputDirectory(string baseOutputFilePath)
        {
            try
            {
                var baseOutputFile = new FileInfo(baseOutputFilePath);
                if (baseOutputFile.Directory == null ||
                    baseOutputFile.DirectoryName == null)
                {
                    OnErrorEvent("Unable to determine the parent directory of the base output file: " + baseOutputFilePath);
                    return false;
                }

                if (!baseOutputFile.Directory.Exists)
                {
                    OnStatusEvent("Creating the output directory: " + baseOutputFile.Directory.FullName);
                    baseOutputFile.Directory.Create();
                }

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Unable to determine the parent directory of the base output file: " + baseOutputFilePath);
                Console.WriteLine(StackTraceFormatter.GetExceptionStackTraceMultiLine(ex));
                return false;
            }
        }
    }
}
