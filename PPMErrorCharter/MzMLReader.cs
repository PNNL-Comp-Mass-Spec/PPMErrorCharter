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

        private readonly string MzMLFilePath;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filePath">Path to mzML file</param>
        public MzMLReader(string filePath)
        {
            MzMLFilePath = filePath;
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

            using var reader = new SimpleMzMLReader(MzMLFilePath);

            var spectraRead = 0;
            var lastStatus = DateTime.UtcNow;

            var maxScanId = psmResults.Max(x => x.ScanIdInt);

            foreach (var spectrum in reader.ReadAllSpectra(false))
            {
                spectraRead++;

                var nativeIdScanNumber = spectrum.NativeIdScanNumber;
                if (nativeIdScanNumber > 0 && nativeIdScanNumber > maxScanId)
                {
                    // All of the PSMs have been processed
                    break;
                }

                var spectrumScanNumber = nativeIdScanNumber == 0 ? spectrum.ScanNumber : nativeIdScanNumber;

                if (spectrum.MsLevel <= 1)
                {
                    continue;
                }

                // Find PSMs associated with the current spectrum
                // First lookup using NativeId
                // If no match, lookup using the scan number

                List<IdentData> psmsForSpectrum;
                if (dataByNativeId.TryGetValue(spectrum.NativeId, out var psmsFromNativeId))
                {
                    psmsForSpectrum = psmsFromNativeId;
                }
                else if (dataByScan.TryGetValue(spectrumScanNumber, out var psmsFromScanNumber))
                {
                    psmsForSpectrum = psmsFromScanNumber;
                }
                else
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
                    else if (spectrum.Precursors.Count > 0 && spectrum.Precursors[0].SelectedIons.Count > 0)
                    {
                        psm.ExperMzRefined = spectrum.Precursors[0].SelectedIons[0].SelectedIonMz;
                    }
                    else
                    {
                        OnWarningEvent("Could not determine the experimental precursor m/z for scan {0}", spectrumScanNumber);
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
