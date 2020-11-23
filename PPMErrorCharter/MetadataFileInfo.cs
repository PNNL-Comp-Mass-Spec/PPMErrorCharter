using System.IO;

namespace PPMErrorCharter
{
    public class MetadataFileInfo
    {
        /// <summary>
        /// Base output file, e.g. DatasetName_HCD_01_MZRefinery
        /// </summary>
        public FileInfo BaseOutputFile { get; }

        /// <summary>
        /// Histogram plot file
        /// </summary>
        /// <remarks>Will be null if Options.HistogramPlotFilePath is not defined</remarks>
        public FileInfo HistogramPlotFile { get; set; }

        /// <summary>
        /// Mass errors plot file
        /// </summary>
        /// <remarks>Will be null if Options.MassErrorPlotFilePath is not defined</remarks>
        public FileInfo MassErrorPlotFile { get; set; }

        /// <summary>
        /// Histogram data, e.g. DatasetName_HCD_01_MZRefinery_Histograms_TmpExportData.txt
        /// </summary>
        public string ErrorHistogramsExportFileName { get; set; }

        /// <summary>
        /// Mass error vs. time data, e.g. DatasetName_HCD_01_MZRefinery_MassErrorsVsTime_TmpExportData.txt
        /// </summary>
        public string MassErrorVsTimeExportFileName { get; set; }

        /// <summary>
        /// Mass error vs. mass data, e.g. DatasetName_HCD_01_MZRefinery_MassErrorsVsMass_TmpExportData.txt
        /// </summary>
        public string MassErrorVsMassExportFileName { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseOutputFilePath"></param>
        /// <param name="options"></param>
        public MetadataFileInfo(string baseOutputFilePath, ErrorCharterOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.HistogramPlotFilePath))
            {
                HistogramPlotFile = new FileInfo(options.HistogramPlotFilePath);
            }

            if (!string.IsNullOrWhiteSpace(options.MassErrorPlotFilePath))
            {
                MassErrorPlotFile = new FileInfo(options.MassErrorPlotFilePath);
            }

            if (string.IsNullOrWhiteSpace(baseOutputFilePath))
            {
                if (HistogramPlotFile?.DirectoryName == null)
                {
                    BaseOutputFile = new FileInfo("Placeholder");
                }
                else
                {
                    BaseOutputFile = new FileInfo(Path.Combine(HistogramPlotFile?.DirectoryName, "Placeholder"));
                }
            }
            else
            {
                BaseOutputFile = new FileInfo(baseOutputFilePath);

                if (HistogramPlotFile == null)
                {
                    HistogramPlotFile = new FileInfo(baseOutputFilePath + "_MZRefinery_Histograms.png");
                }

                if (MassErrorPlotFile == null)
                {
                    MassErrorPlotFile = new FileInfo(baseOutputFilePath + "_MZRefinery_MassErrors.png");
                }
            }
        }
    }
}
