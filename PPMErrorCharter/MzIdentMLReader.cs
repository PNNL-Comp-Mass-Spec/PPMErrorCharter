using System;
using System.Collections.Generic;
using PRISM;
using PSI_Interface.IdentData;

namespace PPMErrorCharter
{
    /// <summary>
    /// Read and perform some processing on a MZIdentML file
    /// Processes the data into an LCMS DataSet
    /// </summary>
    public class MzIdentMLReader : EventNotifier
    {
        // Ignore Spelling: MZIdentML, MyriMatch

        /// <summary>
        /// Default SpecEValue filter threshold
        /// </summary>
        public const double DEFAULT_SPEC_EVALUE_THRESHOLD = 1e-10;

        private readonly double IsotopeErrorFilterWindow = 0.2;
        private readonly double PpmErrorFilterWindow = 50.0;

        private readonly int _maxSteps = 3;
        private int _currentSteps;

        // MS-GF+ Filtering

        /// <summary>
        /// MS-GF+ spec e-value filter
        /// </summary>
        /// <remarks>Keep data less than this value</remarks>
        private double _specEValueThreshold = DEFAULT_SPEC_EVALUE_THRESHOLD;

        /// <summary>
        /// Factor to change the MS-GF+ filter by if not enough matches pass the filter
        /// The filter threshold will be adjusted up to three times
        /// </summary>
        /// <remarks>Multiply by this value</remarks>
        private readonly double _specEValueThresholdStep = 10;

        /// <summary>
        /// MyriMatch filter threshold
        /// </summary>
        /// <remarks>Keep data greater than this value</remarks>
        private readonly double _mvhThreshold = 35;

        private bool AdjustThreshold()
        {
            if (IdentProg == IdentProgramType.MyriMatch)
            {
                return false;
            }
            if (_currentSteps < _maxSteps)
            {
                _currentSteps++;
                _specEValueThreshold *= _specEValueThresholdStep;
                return true;
            }
            return false;
        }

        public bool HaveScanTimes { get; private set; }

        private enum IdentProgramType : byte
        {
            MSGFPlus,
            MyriMatch,
            Unset
        }

        private IdentProgramType IdentProg { get; set; }

        private bool PassesThreshold(IdentData data)
        {
            if (IdentProg == IdentProgramType.MSGFPlus)
            {
                return data.ThresholdValue <= _specEValueThreshold;
            }
            if (IdentProg == IdentProgramType.MyriMatch)
            {
                return data.ThresholdValue >= _mvhThreshold;
            }
            // If neither, return everything
            return true;
        }

        private bool PassesWindows(IdentData data)
        {
            return -IsotopeErrorFilterWindow < data.MassError && data.MassError < IsotopeErrorFilterWindow &&
                   -PpmErrorFilterWindow < data.PpmError && data.PpmError < PpmErrorFilterWindow;
        }

        /// <summary>
        /// Constructor, using an explicit threshold
        /// </summary>
        /// <param name="setThreshold"></param>
        public MzIdentMLReader(double setThreshold)
        {
            IdentProg = IdentProgramType.Unset;

            // Disable threshold stepping by setting currentSteps beyond maxSteps
            _currentSteps = _maxSteps + 1;

            _specEValueThreshold = setThreshold;
            _mvhThreshold = setThreshold;
            HaveScanTimes = false;
        }

        /// <summary>
        /// Constructor, using default thresholds
        /// </summary>
        public MzIdentMLReader()
        {
            IdentProg = IdentProgramType.Unset;
            _currentSteps = 0;
        }

        /// <summary>
        /// Read the MZIdentML file and cache the data
        /// </summary>
        /// <param name="path">Path to *.mzid/mzIdentML file</param>
        /// <returns>List of ScanData</returns>
        public List<IdentData> Read(string path)
        {
            var psmResults = new List<IdentData>();

            // Read in the file
            var mzIdentMLData = new SimpleMZIdentMLReader().Read(path);

            HaveScanTimes = false;

            switch (mzIdentMLData.AnalysisSoftware)
            {
                case "MyriMatch":
                    IdentProg = IdentProgramType.MyriMatch;
                    break;
                case "MS-GF+":
                    IdentProg = IdentProgramType.MSGFPlus;
                    break;
                default:
                    IdentProg = IdentProgramType.Unset;
                    break;
            }

            while (true)
            {
                psmResults.Clear();

                // Filter and process the data
                foreach (var result in mzIdentMLData.Identifications)
                {
                    ProcessSpectrumIdentificationResult(result, psmResults);
                }

                if (psmResults.Count >= 500)
                {
                    OnStatusEvent(string.Format("  {0:N0} PSMs passed the filters", psmResults.Count));
                    break;
                }

                OnStatusEvent(string.Format("  Fewer than 500 PSMs passed the filters ({0})", psmResults.Count));

                // Loosen the filters and try again (up to 3 times)
                if (!AdjustThreshold())
                {
                    if (psmResults.Count == 0)
                        OnWarningEvent("  No PSMs passed the filters");
                    else
                        OnStatusEvent("  Plotting errors using these PSMs");
                    break;
                }

                OnStatusEvent("  Loosening thresholds and trying again");
            }

            return psmResults;
        }

        /// <summary>
        /// Handle a single SpectrumIdentificationResult element and child nodes
        /// Called by ReadSpectrumIdentificationList (xml hierarchy)
        /// </summary>
        /// <param name="result">PSM result</param>
        /// <param name="psmResults"></param>
        private void ProcessSpectrumIdentificationResult(SimpleMZIdentMLReader.SpectrumIdItem result, ICollection<IdentData> psmResults)
        {
            var data = new IdentData
            {
                NativeId = result.NativeId,
                ScanId = (ulong)result.ScanNum,
                ScanTimeSeconds = result.ScanTimeMinutes * 60,
                CalcMz = result.CalMz,
                ExperMz = result.ExperimentalMz,
                Charge = result.Charge,
                ThresholdValue = result.SpecEv  // MS-GF:SpecEValue
            };

            //data.MassError = data.ExperMz - data.CalcMz;
            //data.PpmError = (data.MassError / data.CalcMz) * 1.0e6;

            if (IdentProg == IdentProgramType.MyriMatch)
            {
                if (result.AllParamsDict.TryGetValue("MyriMatch:MVH", out var valueText))
                {
                    if (double.TryParse(valueText, out var value))
                    {
                        data.ThresholdValue = value;
                    }
                }
            }

            if (PassesThreshold(data) && PassesWindows(data))
            {
                psmResults.Add(data);
            }

            if (data.ScanTimeSeconds > 0)
            {
                HaveScanTimes = true;
            }
        }
    }
}
