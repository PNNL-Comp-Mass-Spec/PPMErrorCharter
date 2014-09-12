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
			fixedDataFile += "_FIXED.mzML";
			bool dataFileExists = true;
			if (File.Exists(fixedDataFile + ".gz"))
			{
				fixedDataFile += ".gz";
			}
			else if (!File.Exists(fixedDataFile))
			{
				Console.WriteLine("Warning: Could not find fixed data file named \"" + fixedDataFile + "[.gz]\".");
				Console.WriteLine("\tOuput will not include fixed data graphs.");
				dataFileExists = false;
			}
			string outFile;
			string outFile2;
			//outFile = Path.GetFileName(identFile);
			//outFile = outFile.Replace('.', '_') + "_m_z_calibration.png";
			outFile = Path.ChangeExtension(identFile, "_MZRefinery_MassErrors.png");
			outFile = outFile.Replace("._", "_"); // The Path.ChangeExtension functions leave the '.', so change it now.
			outFile2 = Path.ChangeExtension(identFile, "_MZRefinery_Histograms.png");
			outFile2 = outFile2.Replace("._", "_"); // The Path.ChangeExtension functions leave the '.', so change it now.

			var scanData = MzIdentMLReader.Read(identFile);
			if (dataFileExists)
			{
				MzMLReader.ReadMzMl(fixedDataFile, scanData);
			}


			IdentDataPlotter.ErrorScatterPlotsToPng(scanData, outFile, dataFileExists);
			IdentDataPlotter.ErrorHistogramsToPng(scanData, outFile2, dataFileExists);
		}
	}
}
