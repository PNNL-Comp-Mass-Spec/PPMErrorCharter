using System;
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

			IdentDataPlotter.ErrorScatterPlotsToPng(scanData, outFileStub + "_MZRefinery_MassErrors.png", dataFileExists);
			IdentDataPlotter.ErrorHistogramsToPng(scanData, outFileStub + "_MZRefinery_Histograms.png", dataFileExists);
		}
	}
}
