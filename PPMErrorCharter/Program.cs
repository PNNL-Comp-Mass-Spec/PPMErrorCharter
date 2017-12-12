﻿using System;
using System.IO;

namespace PPMErrorCharter
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: " + System.AppDomain.CurrentDomain.FriendlyName + " file.mzid[.gz] [specEValueThreshold]");
                return;
            }

            // Set a solid, unchanging threshold if the user specifies one.
            var useSetThreshold = false;
            double setThreshold = 0;
            if (args.Length > 1)
            {
                if (double.TryParse(args[1], out var possibleThreshold))
                {
                    setThreshold = possibleThreshold;
                    useSetThreshold = true;
                }
            }
            // Get the file name
            if (!(identFile.EndsWith(".mzid") || identFile.EndsWith(".mzid.gz")))
            var identFilePath = args[0];
            {
                Console.WriteLine("Error: \"" + identFile + "\" is not an mzIdentML file.");
                return;
            }
            string fixedDataFile = identFile.Substring(0, identFile.LastIndexOf(".mzid"));
            if (fixedDataFile.EndsWith("_msgfplus"))
            var identFile = new FileInfo(identFilePath);
            if (!identFile.Exists)
            {
                fixedDataFile = fixedDataFile.Substring(0, fixedDataFile.LastIndexOf("_msgfplus"));
                Console.WriteLine("Error: Data file not found: \"" + identFilePath + "\"");
                if (!Path.IsPathRooted(identFilePath))
                    Console.WriteLine("Full file path: " + identFile.FullName);

                System.Threading.Thread.Sleep(1500);
                return;
            }
            string outFileStub = fixedDataFile;
            fixedDataFile += "_FIXED.mzML";
            bool dataFileExists = true;
            if (File.Exists(fixedDataFile + ".gz"))
            {
                fixedDataFile += ".gz";
            }
            else if (!File.Exists(fixedDataFile))
            {
                dataFileExists = false;
            }
            Console.WriteLine("Creating plots for \"" + identFile + "\"");
            if (dataFileExists)
            {
                Console.WriteLine("\tUsing fixed data file \"" + fixedDataFile + "\"");
            }
            else
            {
                Console.WriteLine("\tWarning: Could not find fixed data file \"" + fixedDataFile + "[.gz]\".");
                Console.WriteLine("\tOutput will not include fixed data graphs.");
            }

            MzIdentMLReader reader;
            if (useSetThreshold)
            {
                reader = new MzIdentMLReader(setThreshold);
            }
            else
            {
                reader = new MzIdentMLReader();
            }
            var scanData = reader.Read(identFile.FullName);
            var haveScanTimes = reader.HaveScanTimes;
            if (dataFileExists)
            {
                var mzML = new MzMLReader(fixedDataFile);
                mzML.ReadSpectraData(scanData);
                haveScanTimes = true;
            }

            var stats = new IdentDataStats(scanData);

            stats.PrintStatsTable();

            var origSize = scanData.Count;
            var itemsRemoved = 0;
            for (var i = 0; i < scanData.Count; i++)
            {
                if (scanData[i].OutOfRange())
                {
                    scanData.RemoveAt(i);
                    i--; // Step back one value, to hit this same index again
                    itemsRemoved++;
                }
            }
            Console.WriteLine("Removed " + itemsRemoved + " out-of-range items from the original " + origSize + " items.");

            IdentDataPlotter.ErrorScatterPlotsToPng(scanData, outFileStub + "_MZRefinery_MassErrors.png", dataFileExists, haveScanTimes);
            //IdentDataPlotter.ErrorScatterPlotsToPng(scanData, outFileStub + "_MZRefinery_MassErrors.png", dataFileExists, false);
            IdentDataPlotter.ErrorHistogramsToPng(scanData, outFileStub + "_MZRefinery_Histograms.png", dataFileExists);

            /*using (var file = new StreamWriter(new FileStream(outFileStub + "_debug.tsv", FileMode.Create, FileAccess.Write, FileShare.None)))
            {
                file.WriteLine("NativeID\tCalcMZ\tExperMZ\tRefineMZ\tMassError\tPpmError\tRMassError\tRPpmError\tCharge");
                foreach (var data in scanData)
                {
                    double error = data.MassErrorIsotoped - data.MassErrorRefinedIsotoped;
                    if (error < -0.2 || 0.2 < error)
                    {
                        file.WriteLine(data.ToDebugString());
                    }
                }
            }*/
        }
    }
}
