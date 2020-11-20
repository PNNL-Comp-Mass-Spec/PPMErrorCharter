using System;
using System.Collections.Generic;

namespace PPMErrorCharter
{
    public class IdentDataStats
    {
        // Ignore Spelling: PpmErrorIsotoped

        private readonly List<IdentData> _data;
        public double Mean { get; private set; }
        public double Median { get; private set; }
        public double StDev { get; private set; }
        public double StDevMedian { get; private set; }
        public double Percent99 { get; private set; }

        public double RefinedMean { get; private set; }
        public double RefinedMedian { get; private set; }
        public double RefinedStDev { get; private set; }
        public double RefinedStDevMedian { get; private set; }
        public double RefinedPercent99 { get; private set; }

        public IdentDataStats(List<IdentData> newData)
        {
            _data = newData;
            RefinedMean = -500;
            RefinedMedian = -500;
            RefinedStDev = -500;
            RefinedStDevMedian = -500;
            GetStats(false);
        }

        public void PrintStatsTable()
        {
            const int widthTitle = -25; // Negative to left-align
            const int widthOrig = 10;
            const int widthRefined = 10;
            const string decStr = "F3"; // 3 decimal places
            var formatStringFlt = "\t{0," + widthTitle + "} {1," + widthOrig + ":" + decStr + "} {2," + widthRefined + ":" + decStr + "}";
            var formatStringStr = "\t{0," + widthTitle + "} {1," + widthOrig + "} {2," + widthRefined + "}";
            Console.WriteLine(formatStringStr, "Statistic", "Original", "Refined");
            Console.WriteLine(formatStringFlt, "MeanMassErrorPPM:", Mean, RefinedMean);
            Console.WriteLine(formatStringFlt, "MedianMassErrorPPM:", Median, RefinedMedian);
            Console.WriteLine(formatStringFlt, "StDev(Mean):", StDev, RefinedStDev);
            Console.WriteLine(formatStringFlt, "StDev(Median):", StDevMedian, RefinedStDevMedian);
            Console.WriteLine(formatStringFlt, "PPM Window for 99%: 0 +/-", Math.Abs(Median) + (StDevMedian * 3), Math.Abs(RefinedMedian) + (RefinedStDevMedian * 3));
            Console.WriteLine(formatStringFlt, "PPM Window for 99%: high:", Median + (StDevMedian * 3), RefinedMedian + (RefinedStDevMedian * 3));
            Console.WriteLine(formatStringFlt, "PPM Window for 99%:  low:", Median - (StDevMedian * 3), RefinedMedian - (RefinedStDevMedian * 3));
        }

        private void GetStats(bool useIsotoped)
        {
            GetMeans(useIsotoped);
            GetMedians(useIsotoped);
            GetStdDev(useIsotoped);
            GetPercent99s();
        }

        private void GetMeans(bool useIsotoped)
        {
            double sum = 0;
            double sumRefined = 0;
            foreach (var data in _data)
            {
                if (useIsotoped)
                {
                    sum += data.PpmErrorIsotoped;
                    sumRefined += data.PpmErrorRefinedIsotoped;
                }
                else
                {
                    sum += data.PpmError;
                    sumRefined += data.PpmErrorRefined;
                }
            }

            Mean = sum / _data.Count;
            RefinedMean = sumRefined / _data.Count;
        }

        private void GetMedians(bool useIsotoped)
        {
            if (useIsotoped)
            {
                // Sort by the PpmErrorIsotoped
                _data.Sort(new IdentDataByPpmErrorIsotoped());
                Median = _data[_data.Count / 2].PpmErrorIsotoped;

                // Sort by the fixed PpmErrorRefinedIsotoped
                _data.Sort(new IdentDataByPpmErrorIsotopedRefined());
                RefinedMedian = _data[_data.Count / 2].PpmErrorRefinedIsotoped;
            }
            else
            {
                // Sort by the PpmError
                _data.Sort(new IdentDataByPpmErrorIsotopedRefined());
                Median = _data[_data.Count / 2].PpmError;

                // Sort by the fixed PpmError
                _data.Sort(new IdentDataByPpmErrorRefined());
                RefinedMedian = _data[_data.Count / 2].PpmErrorRefined;
            }
        }

        private void GetStdDev(bool useIsotoped)
        {
            double sumVar = 0;
            double sumVarMed = 0;
            double sumVarRef = 0;
            double sumVarMedRef = 0;

            // Calculate Variance: average of squared differences from center point
            foreach (var data in _data)
            {
                if (useIsotoped)
                {
                    sumVar += Math.Pow(data.PpmErrorIsotoped - Mean, 2);
                    sumVarMed += Math.Pow(data.PpmErrorIsotoped - Median, 2);
                    sumVarRef += Math.Pow(data.PpmErrorRefinedIsotoped - RefinedMean, 2);
                    sumVarMedRef += Math.Pow(data.PpmErrorRefinedIsotoped - RefinedMedian, 2);
                }
                else
                {
                    sumVar += Math.Pow(data.PpmError - Mean, 2);
                    sumVarMed += Math.Pow(data.PpmError - Median, 2);
                    sumVarRef += Math.Pow(data.PpmErrorRefined - RefinedMean, 2);
                    sumVarMedRef += Math.Pow(data.PpmErrorRefined - RefinedMedian, 2);
                }
            }

            var var = sumVar / _data.Count;
            var varMed = sumVarMed / _data.Count;
            var varRef = sumVarRef / _data.Count;
            var varMedRef = sumVarMedRef / _data.Count;

            // Calculate Standard Deviation: square root of variance
            StDev = Math.Sqrt(var);
            StDevMedian = Math.Sqrt(varMed);
            RefinedStDev = Math.Sqrt(varRef);
            RefinedStDevMedian = Math.Sqrt(varMedRef);
        }

        private void GetPercent99s()
        {
            Percent99 = StDevMedian * 3;
            RefinedPercent99 = RefinedStDevMedian * 3;
        }
    }
}
