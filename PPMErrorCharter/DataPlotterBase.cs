using System;
using System.Collections.Generic;
using PRISM;

namespace PPMErrorCharter
{
    /// <summary>
    /// Base class for mass error plot generation
    /// </summary>
    public abstract class DataPlotterBase : clsEventNotifier
    {
        /// <summary>
        /// Base output fle path
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
        /// <returns></returns>
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
    }
}
