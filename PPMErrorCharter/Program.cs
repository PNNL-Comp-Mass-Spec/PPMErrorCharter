using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace PPMErrorCharter
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine("Usage: " + System.AppDomain.CurrentDomain.FriendlyName + " file.mzid[.gz]");
				return;
			}
			string identFile = args[0];
			if (!(identFile.EndsWith(".mzid") || identFile.EndsWith(".mzid.gz")))
			{
				Console.WriteLine("Error: \"" + identFile + "\" is not an mzIdentML file.");
				return;
			}
			string fixedDataFile = identFile.Substring(0, identFile.LastIndexOf(".mzid"));
			if (fixedDataFile.EndsWith("_msgfplus"))
			{
				fixedDataFile = fixedDataFile.Substring(0, fixedDataFile.LastIndexOf("_msgfplus"));
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
				Console.WriteLine("\tOuput will not include fixed data graphs.");
			}

			var scanData = MzIdentMLReader.Read(identFile);
			if (dataFileExists)
			{
				MzMLReader.ReadMzMl(fixedDataFile, scanData);
			}

			scanData.Sort(new IdentDataByPpmError()); // Sort by the PpmError
			Console.WriteLine("\tMedianMassErrorPPM: " + Math.Round(scanData[scanData.Count / 2].PpmError, 3));
			scanData.Sort(new IdentDataByPpmErrorRefined()); // Sort by the fixed PpmError
			Console.WriteLine("\tMedianMassErrorPPM_Refined: " + Math.Round(scanData[scanData.Count / 2].PpmErrorRefined, 3));

			int origSize = scanData.Count;
			int itemsRemoved = 0;
			for (int i = 0; i < scanData.Count; i++)
			{
				if (scanData[i].OutOfRange())
				{
					scanData.RemoveAt(i);
					i--; // Step back one value, to hit this same index again
					itemsRemoved++;
				}
			}
			Console.WriteLine("Removed " + itemsRemoved + " items from data set of " + origSize + " items to keep results nice.");

			IdentDataPlotter.ErrorScatterPlotsToPng(scanData, outFileStub + "_MZRefinery_MassErrors.png", dataFileExists);
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
