using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PRISM;

namespace PPMErrorCharter
{
    /// <summary>
    /// Generate mass error plots using Python
    /// </summary>
    public class PythonDataPlotter : DataPlotterBase
    {
        // Ignore Spelling: tmp, autoscale, gridline, usr, png

        protected const string TMP_FILE_SUFFIX = "_TmpExportData";

        /// <summary>
        /// When true, delete the temporary text files that contain data for Python to plot
        /// </summary>
        public bool DeleteTempFiles { get; set; }

        /// <summary>
        /// Path to the python executable
        /// </summary>
        public static string PythonPath { get; private set; }

        /// <summary>
        /// True if the Python .exe could be found, otherwise false
        /// </summary>
        public static bool PythonInstalled => FindPython();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="baseOutputFilePath"></param>
        public PythonDataPlotter(ErrorCharterOptions options, string baseOutputFilePath) : base(options, baseOutputFilePath)
        {
            DeleteTempFiles = true;

            if (PythonPath == null)
                PythonPath = string.Empty;
        }

        /// <summary>
        /// Generate the mass error scatter plots and save to a PNG file
        /// </summary>
        /// <param name="scanData"></param>
        /// <param name="fixedMzMLFileExists"></param>
        /// <param name="haveScanTimes"></param>
        /// <param name="baseOutputFile"></param>
        /// <param name="massErrorVsTimeExportFileName"></param>
        /// <param name="massErrorVsMassExportFileName"></param>
        private bool ExportScatterPlotData(
            IReadOnlyCollection<IdentData> scanData,
            bool fixedMzMLFileExists,
            bool haveScanTimes,
            FileInfo baseOutputFile,
            out string massErrorVsTimeExportFileName,
            out string massErrorVsMassExportFileName)
        {
            if (!PythonInstalled)
            {
                NotifyPythonNotFound("Could not find the python executable");
                massErrorVsTimeExportFileName = string.Empty;
                massErrorVsMassExportFileName = string.Empty;
                return false;
            }

            if (baseOutputFile.Directory == null || baseOutputFile.DirectoryName == null)
            {
                OnErrorEvent("Unable to determine the parent directory of the base output file: " + baseOutputFile.FullName);
                massErrorVsTimeExportFileName = string.Empty;
                massErrorVsMassExportFileName = string.Empty;
                return false;
            }

            massErrorVsTimeExportFileName = Path.GetFileNameWithoutExtension(baseOutputFile.Name) + "_MassErrorsVsTime" + TMP_FILE_SUFFIX + ".txt";
            var massErrorVsTimeDataExportFile = new FileInfo(Path.Combine(baseOutputFile.DirectoryName, massErrorVsTimeExportFileName));

            // Export data for the mass errors vs. Time plot
            using (var writer = new StreamWriter(new FileStream(massErrorVsTimeDataExportFile.FullName, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                writer.WriteLine("[Title1=Scan Time: Original;Title2=Scan Time: Refined]");
                writer.WriteLine("Autoscale=true;Minimum=0;Maximum=1496298880;StringFormat=#,##0;MinorGridlineThickness=0;MajorStep=1	Autoscale=false;Minimum=-20;Maximum=20;StringFormat=#,##0;MinorGridlineThickness=0;MajorStep=1");

                var headerColumns = new List<string>
                {
                    "Scan Time (minutes)",
                    "Original: Mass Error (PPM)",
                    "Refined: Mass Error (PPM)"
                };

                writer.WriteLine(string.Join("\t", headerColumns));

                if (haveScanTimes)
                {
                    foreach (var item in from x in scanData orderby x.ScanTimeSeconds select x)
                    {
                        var ppmErrorRefined = fixedMzMLFileExists ? StringUtilities.DblToString(item.PpmErrorRefined, 3) : string.Empty;

                        writer.WriteLine("{0}\t{1}\t{2}",
                            StringUtilities.DblToString(item.ScanTimeSeconds, 3),
                            StringUtilities.DblToString(item.PpmError, 3),
                            ppmErrorRefined);
                    }
                }
                else
                {
                    foreach (var item in from x in scanData orderby x.ScanIdInt select x)
                    {
                        var ppmErrorRefined = fixedMzMLFileExists ? StringUtilities.DblToString(item.PpmErrorRefined, 3) : string.Empty;

                        writer.WriteLine("{0}\t{1}\t{2}",
                            item.ScanIdInt,
                            StringUtilities.DblToString(item.PpmError, 3),
                            ppmErrorRefined);
                    }
                }
            }

            massErrorVsMassExportFileName = Path.GetFileNameWithoutExtension(baseOutputFile.Name) + "_MassErrorsVsMass" + TMP_FILE_SUFFIX + ".txt";
            var massErrorVsMassDataExportFile = new FileInfo(Path.Combine(baseOutputFile.DirectoryName, massErrorVsMassExportFileName));

            using (var writer = new StreamWriter(new FileStream(massErrorVsMassDataExportFile.FullName, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                writer.WriteLine("[Title1=M/Z: Original;Title2=M/Z: Refined]");
                writer.WriteLine("Autoscale=true;Minimum=0;Maximum=1496298880;StringFormat=#,##0;MinorGridlineThickness=0;MajorStep=1	Autoscale=false;Minimum=-20;Maximum=20;StringFormat=#,##0;MinorGridlineThickness=0;MajorStep=1");

                var headerColumns = new List<string>
                {
                    "m/z",
                    "Original: Mass Error (PPM)",
                    "Refined: Mass Error (PPM)"
                };

                writer.WriteLine(string.Join("\t", headerColumns));

                foreach (var item in from x in scanData orderby x.CalcMz select x)
                {
                    var ppmErrorRefined = fixedMzMLFileExists ? StringUtilities.DblToString(item.PpmErrorRefined, 3) : string.Empty;

                    writer.WriteLine("{0}\t{1}\t{2}",
                        StringUtilities.DblToString(item.CalcMz, 4),
                        StringUtilities.DblToString(item.PpmError, 3),
                        ppmErrorRefined);
                }
            }

            return true;
        }

        /// <summary>
        /// Generate the mass error histogram plots and save to a PNG file
        /// </summary>
        /// <param name="scanData"></param>
        /// <param name="baseOutputFile"></param>
        /// <param name="fixedMzMLFileExists"></param>
        /// <param name="massErrorHistogramsExportFileName"></param>
        private bool ExportHistogramPlotData(
            IReadOnlyCollection<IdentData> scanData,
            FileInfo baseOutputFile,
            bool fixedMzMLFileExists,
            out string massErrorHistogramsExportFileName)
        {
            if (!PythonInstalled)
            {
                NotifyPythonNotFound("Could not find the python executable");
                massErrorHistogramsExportFileName = string.Empty;
                return false;
            }

            if (baseOutputFile.DirectoryName == null)
            {
                OnErrorEvent("Unable to determine the parent directory of the base output file: " + baseOutputFile.FullName);
                massErrorHistogramsExportFileName = string.Empty;
                return false;
            }

            var massErrorHistogram = HistogramFrequencies(scanData, "PpmError");

            var refinedMassErrorHistogram = HistogramFrequencies(scanData, "PpmErrorRefined");

            var mergedHistogramData = new SortedDictionary<double, MassErrorHistogramResult>();

            foreach (var item in massErrorHistogram)
            {
                mergedHistogramData.Add(item.Key, new MassErrorHistogramResult(item.Value));
            }

            foreach (var item in refinedMassErrorHistogram)
            {
                if (mergedHistogramData.TryGetValue(item.Key, out var binData))
                {
                    binData.BinCountRefined = item.Value;
                }
                else
                {
                    mergedHistogramData.Add(item.Key, new MassErrorHistogramResult(0, item.Value));
                }
            }

            massErrorHistogramsExportFileName = Path.GetFileNameWithoutExtension(baseOutputFile.Name) + "_Histograms" + TMP_FILE_SUFFIX + ".txt";
            var massErrorHistogramsExportFile = new FileInfo(Path.Combine(baseOutputFile.DirectoryName, massErrorHistogramsExportFileName));

            // Export data for the mass errors vs. Time plot
            using var writer = new StreamWriter(new FileStream(massErrorHistogramsExportFile.FullName, FileMode.Create, FileAccess.Write, FileShare.Read));

            writer.WriteLine("[Title1=Original;Title2=Refined]");
            writer.WriteLine("Autoscale=false;Minimum=-50;Maximum=50;StringFormat=#,##0;MinorGridlineThickness=0;MajorStep=1	Autoscale=true;StringFormat=#,##0;MinorGridlineThickness=0;MajorStep=1");

            var headerColumns = new List<string>
            {
                "Mass error (PPM)",
                "Original: Counts",
                "Refined: Counts"
            };

            writer.WriteLine(string.Join("\t", headerColumns));

            foreach (var item in mergedHistogramData)
            {
                string refinedMassError;
                if (fixedMzMLFileExists)
                    refinedMassError = item.Value.BinCountRefined.ToString();
                else
                    refinedMassError = string.Empty;

                writer.WriteLine("{0}\t{1}\t{2}",
                    item.Key,
                    item.Value.BinCountOriginal,
                    refinedMassError);
            }

            return true;
        }

        /// <summary>
        /// Find the best candidate folder with Python 3.x
        /// </summary>
        /// <returns>True if Python could be found, otherwise false</returns>
        protected static bool FindPython()
        {
            if (!string.IsNullOrWhiteSpace(PythonPath))
                return true;

            if (PRISM.SystemInfo.IsLinux)
            {
                PythonPath = "/usr/bin/python3";
                ConsoleMsgUtils.ShowDebug("Assuming Python 3 is at {0}", PythonPath);
                return true;
            }

            var pathsToCheck = PythonPathsToCheck();

            foreach (var directoryPath in pathsToCheck)
            {
                var exePath = FindPythonExe(directoryPath);
                if (string.IsNullOrWhiteSpace(exePath))
                    continue;

                PythonPath = exePath;
                break;
            }

            return !string.IsNullOrWhiteSpace(PythonPath);
        }

        /// <summary>
        /// Find the best candidate folder with Python 3.x
        /// </summary>
        /// <returns>Path to the python executable, otherwise an empty string</returns>
        private static string FindPythonExe(string directoryPath)
        {
            var directory = new DirectoryInfo(directoryPath);
            if (!directory.Exists)
                return string.Empty;

            var subDirectories = directory.GetDirectories("Python3*").ToList();
            subDirectories.AddRange(directory.GetDirectories("Python 3*"));
            subDirectories.Add(directory);

            var candidates = new List<FileInfo>();

            foreach (var subDirectory in subDirectories)
            {
                var files = subDirectory.GetFiles("python.exe");
                if (files.Length == 0)
                    continue;

                candidates.Add(files[0]);
            }

            if (candidates.Count == 0)
                return string.Empty;

            // Find the newest .exe
            var query = (from item in candidates orderby item.LastWriteTime select item.FullName);

            return query.First();
        }

        /// <summary>
        /// Generate the mass error scatter plots and mass error histogram plots, saving as PNG files
        /// </summary>
        /// <param name="scanData"></param>
        /// <param name="fixedMzMLFileExists"></param>
        /// <param name="haveScanTimes"></param>
        public override bool GeneratePNGPlots(IReadOnlyCollection<IdentData> scanData, bool fixedMzMLFileExists, bool haveScanTimes)
        {
            var metadataFileInfo = new MetadataFileInfo(BaseOutputFilePath, Options);

            if (!ValidateOutputDirectories(BaseOutputFilePath))
                return false;

            var histogramPlotDataExported = ExportHistogramPlotData(
                scanData, metadataFileInfo.BaseOutputFile, fixedMzMLFileExists,
                out var errorHistogramsExportFileName);

            if (!histogramPlotDataExported)
            {
                return false;
            }

            var scatterPlotDataExported = ExportScatterPlotData(
                scanData, fixedMzMLFileExists, haveScanTimes, metadataFileInfo.BaseOutputFile,
                out var massErrorVsTimeExportFileName,
                out var massErrorVsMassExportFileName);

            if (!scatterPlotDataExported)
            {
                return false;
            }

            metadataFileInfo.ErrorHistogramsExportFileName = errorHistogramsExportFileName;
            metadataFileInfo.MassErrorVsTimeExportFileName = massErrorVsTimeExportFileName;
            metadataFileInfo.MassErrorVsMassExportFileName = massErrorVsMassExportFileName;

            var success = GeneratePlotsWithPython(metadataFileInfo);

            return success;
        }

        /// <summary>
        /// Call the Python script to create the plots
        /// </summary>
        /// <returns>True if success, otherwise false</returns>
        /// <remarks>Call ErrorHistogramsToPng and ErrorScatterPlotsToPng prior to calling this method</remarks>
        private bool GeneratePlotsWithPython(MetadataFileInfo metadataFileInfo)
        {
            if (!PythonInstalled)
            {
                NotifyPythonNotFound("Could not find the python executable");
                return false;
            }

            if (metadataFileInfo.BaseOutputFile.Directory == null)
            {
                OnErrorEvent("Unable to determine the parent directory of the base output file: " + metadataFileInfo.BaseOutputFile.FullName);
                return false;
            }

            if (!metadataFileInfo.BaseOutputFile.Directory.Exists)
            {
                metadataFileInfo.BaseOutputFile.Directory.Create();
            }

            var workDir = metadataFileInfo.BaseOutputFile.Directory.FullName;

            var exeDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (exeDirectory == null)
            {
                OnErrorEvent("Unable to determine the path to the directory with the PPMErrorCharter executable");
                return false;
            }

            var pythonScriptFile = new FileInfo(Path.Combine(exeDirectory, "PPMErrorCharter_Plotter.py"));
            if (!pythonScriptFile.Exists)
            {
                OnErrorEvent("Python plotting script not found: " + pythonScriptFile.FullName);
                return false;
            }

            var baseOutputName = metadataFileInfo.BaseOutputFile.Name;

            var metadataFile = new FileInfo(Path.Combine(workDir, "MZRefinery_Plotting_Metadata.txt"));
            OnDebugEvent("Creating " + metadataFile.FullName);

            using (var writer = new StreamWriter(new FileStream(metadataFile.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            {
                var plotFilesDefined = 0;
                if (metadataFileInfo.HistogramPlotFile != null)
                {
                    writer.WriteLine("HistogramPlotFilePath=" + metadataFileInfo.HistogramPlotFile.FullName);
                    plotFilesDefined++;
                }

                if (metadataFileInfo.MassErrorPlotFile != null)
                {
                    writer.WriteLine("MassErrorPlotFilePath=" + metadataFileInfo.MassErrorPlotFile.FullName);
                    plotFilesDefined++;
                }

                if (plotFilesDefined < 2)
                {
                    writer.WriteLine("BaseOutputName=" + baseOutputName);
                    metadataFileInfo.HistogramPlotFile = new FileInfo(Path.Combine(metadataFileInfo.BaseOutputFile.FullName, baseOutputName + "_Histograms.png"));
                    metadataFileInfo.MassErrorPlotFile = new FileInfo(Path.Combine(metadataFileInfo.BaseOutputFile.FullName, baseOutputName + "_MassErrors.png"));
                }

                writer.WriteLine("HistogramData=" + metadataFileInfo.ErrorHistogramsExportFileName);
                writer.WriteLine("MassErrorVsTimeData=" + metadataFileInfo.MassErrorVsTimeExportFileName);
                writer.WriteLine("MassErrorVsMassData=" + metadataFileInfo.MassErrorVsMassExportFileName);
            }

            var args = PathUtils.PossiblyQuotePath(pythonScriptFile.FullName) + " " + PathUtils.PossiblyQuotePath(metadataFile.FullName);

            OnDebugEvent(string.Format("{0} {1}", PythonPath, args));

            var progRunner = new ProgRunner
            {
                Arguments = args,
                CreateNoWindow = true,
                MonitoringInterval = 2000,
                Name = "PythonPlotter",
                Program = PythonPath,
                Repeat = false,
                RepeatHoldOffTime = 0,
                WorkDir = workDir
            };

            RegisterEvents(progRunner);

            const int MAX_RUNTIME_SECONDS = 600;
            const int MONITOR_INTERVAL_MILLISECONDS = 1000;
            var runtimeExceeded = false;

            try
            {
                // Start the program executing
                progRunner.StartAndMonitorProgram();

                var startTime = DateTime.UtcNow;

                // Loop until program is complete, or until MAX_RUNTIME_SECONDS seconds elapses
                while (progRunner.State != ProgRunner.States.NotMonitoring)
                {
                    ProgRunner.SleepMilliseconds(MONITOR_INTERVAL_MILLISECONDS);

                    if (DateTime.UtcNow.Subtract(startTime).TotalSeconds < MAX_RUNTIME_SECONDS)
                        continue;

                    OnErrorEvent(string.Format("Plot creation with Python has taken more than {0:F0} minutes; aborting", MAX_RUNTIME_SECONDS / 60.0));
                    progRunner.StopMonitoringProgram(kill: true);

                    runtimeExceeded = true;
                    break;
                }
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception creating plots using Python", ex);
                return false;
            }

            if (runtimeExceeded)
                return false;

            // Examine the exit code
            if (progRunner.ExitCode == 0)
            {
                OnStatusEvent("Generated plots; see:\n  " + metadataFileInfo.HistogramPlotFile?.FullName + "\nand\n  " + metadataFileInfo.MassErrorPlotFile?.FullName);

                if (DeleteTempFiles)
                {
                    // Delete the temp export files

                    try
                    {
                        metadataFile.Delete();
                        File.Delete(Path.Combine(workDir, metadataFileInfo.ErrorHistogramsExportFileName));
                        File.Delete(Path.Combine(workDir, metadataFileInfo.MassErrorVsTimeExportFileName));
                        File.Delete(Path.Combine(workDir, metadataFileInfo.MassErrorVsMassExportFileName));
                    }
                    catch (Exception ex)
                    {
                        OnErrorEvent("Error deleting files: " + ex.Message);
                    }
                }
                else
                {
                    ConsoleMsgUtils.ShowDebug("{0}\n    {1}\n    {2}\n    {3}\n    {4}",
                                              "Not deleting the following temporary files since debug mode is enabled",
                                              metadataFile.FullName,
                                              Path.Combine(workDir, metadataFileInfo.ErrorHistogramsExportFileName),
                                              Path.Combine(workDir, metadataFileInfo.MassErrorVsTimeExportFileName),
                                              Path.Combine(workDir, metadataFileInfo.MassErrorVsMassExportFileName));
                }

                return true;
            }

            OnErrorEvent("Python ExitCode = " + progRunner.ExitCode);
            return false;
        }

        protected void NotifyPythonNotFound(string currentTask)
        {
            OnErrorEvent(currentTask + "; Python not found");

            var debugMsg = "Paths searched:";
            foreach (var item in PythonPathsToCheck())
            {
                debugMsg += "\n  " + item;
            }

            OnDebugEvent(debugMsg);
        }

        public static IEnumerable<string> PythonPathsToCheck()
        {
            return new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
                @"C:\ProgramData\Anaconda3",
                @"C:\"
            };
        }
    }
}