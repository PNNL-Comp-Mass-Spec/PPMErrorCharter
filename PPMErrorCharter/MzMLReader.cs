using System;
using System.Collections.Generic;
using System.Linq;
using PRISM;
using PSI_Interface.MSData;

namespace PPMErrorCharter
{
    public sealed class MzMLReader : EventNotifier
    {
        // Ignore Spelling: psmResults

        private readonly string _mzMLFilePath;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filePath">Path to mzML file</param>
        public MzMLReader(string filePath)
        {
            _mzMLFilePath = filePath;
        }

        public void ReadSpectraData(List<IdentData> psmResults)
        {
            var dataByNativeId = new Dictionary<string, List<IdentData>>();
            var dataByScan = new Dictionary<int, List<IdentData>>();

            foreach (var psm in psmResults)
            {
                if (!string.IsNullOrWhiteSpace(psm.NativeId))
                {
                    if (!dataByNativeId.TryGetValue(psm.NativeId, out var results))
                    {
                        results = new List<IdentData>();
                        dataByNativeId.Add(psm.NativeId, results);
                    }
                    results.Add(psm);
                }

                if (psm.ScanIdInt >= 0)
                {
                    if (!dataByScan.TryGetValue(psm.ScanIdInt, out var results))
                    {
                        results = new List<IdentData>();
                        dataByScan.Add(psm.ScanIdInt, results);
                    }
                    results.Add(psm);
                }
            }

            if (psmResults.Count == 0)
            {
                OnWarningEvent("Empty psmResults were sent to ReadSpectraData; nothing to do");
                return;
            }

            using var reader = new SimpleMzMLReader(_mzMLFilePath);

            var spectraRead = 0;
            var lastStatus = DateTime.UtcNow;

            var maxScanId = psmResults.Max(x => x.ScanIdInt);

            foreach (var spectrum in reader.ReadAllSpectra(false))
            {
                spectraRead++;

                if (spectrum.ScanNumber > maxScanId)
                {
                    // All of the PSMs have been processed
                    break;
                }

                if (spectrum.MsLevel <= 1)
                {
                    continue;
                }

                if (!dataByNativeId.TryGetValue(spectrum.NativeId, out var psmsForSpectrum) &&
                    !dataByScan.TryGetValue(spectrum.ScanNumber, out psmsForSpectrum))
                {
                    continue;
                }

                foreach (var psm in psmsForSpectrum)
                {
                    var experimentalMzRefined = spectrum.GetThermoMonoisotopicMz();

                    if (Math.Abs(experimentalMzRefined) > 0)
                    {
                        psm.ExperMzRefined = experimentalMzRefined;
                    }
                    else
                    {
                        if (spectrum.Precursors.Count > 0 && spectrum.Precursors.First().SelectedIons.Count > 0)
                        {
                            psm.ExperMzRefined = spectrum.Precursors.First().SelectedIons.First().SelectedIonMz;
                        }
                        else
                        {
                            OnWarningEvent(string.Format(
                                "Could not determine the experimental precursor m/z for scan {0}",
                                spectrum.ScanNumber));
                        }
                    }

                    if (psm.ScanTimeSeconds <= 0)
                    {
                        // StartTime is stored in minutes, we've been using seconds.
                        psm.ScanTimeSeconds = spectrum.ScanStartTime * 60;
                    }
                }

                if (DateTime.UtcNow.Subtract(lastStatus).TotalSeconds < 5)
                {
                    continue;
                }

                Console.WriteLine("  {0:F0}% complete", spectraRead / (double)reader.NumSpectra * 100);
                lastStatus = DateTime.UtcNow;
            }
        }
    }
}
