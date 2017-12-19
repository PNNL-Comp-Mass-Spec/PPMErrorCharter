import glob
import math
import matplotlib.patches as mpatches
import matplotlib.pyplot as plt
import matplotlib.ticker as mtick
import matplotlib.colors as mpColors
import os
from pathlib import Path
import numpy as np
import pandas as pd
from pprint import pprint

# -------------------------------------------------------------------------------
# This file plots data generated by PPMErrorCharter.exe, saving plots as PNG files
# The filename specified at the command line must be a text file with the names
# of the data files with the data to plot
#
# Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
# Program started in 2017
#
# E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
# Website: http:#omics.pnl.gov/ or http:#www.sysbio.org/resources/staff/ or http:#panomics.pnnl.gov/
# -------------------------------------------------------------------------------

# Update the default font
plt.rcParams["font.family"] = "arial"
            
def process_file(metadataFilePath):

    metadataFile = Path(metadataFilePath)
    if not metadataFile.is_file():
        print('Error, metadata file not found: ' + metadataFilePath)
        ShowExampleMetadataFile()
        return

    print('Reading metadata file: ' + metadataFilePath)
    
    # Read the filenames in the metadata file
    # It should have 4 lines of information (either filenames or full paths)
    #   BaseOutputName=Output_File_Base_Name
    #   HistogramData=Histogram_Data_File_Name
    #   MassErrorVsTimeData=MassErrorsVsTime_Data_File_Name
    #   MassErrorVsMassData=MassErrorsVsMass_Data_File_Name
    
    metadata = {}
    with open(metadataFile, 'r') as f:
        for line in f:
            if len(line.strip()) == 0:
                continue

            lineParts = line.split("=")
            if len(lineParts) < 2:
                print('Ignoring invalid metadata line: ' + line)
                continue
                
            metadata[lineParts[0].strip()] = lineParts[1].strip()

    if len(metadata) < 2:
        print('\nError: Metadata file has fewer than 2 lines')
        ShowExampleMetadataFile()
        return
    
    print()
 
    if not 'BaseOutputName' in metadata:
        print ('Error: keyword BaseOutputName not found in the metadata file')
        ShowExampleMetadataFile()
        return

    if not 'HistogramData' in metadata:
        print ('Error: keyword HistogramData not found in the metadata file')
        ShowExampleMetadataFile()
        return

    baseName = metadata['BaseOutputName']
    histogramDataFilePath = metadata['HistogramData']

    if 'MassErrorVsTimeData' in metadata and 'MassErrorVsMassData' in metadata:
        errorVsTimeDataFilePath = metadata['MassErrorVsTimeData']
        errorVsMassDataFilePath = metadata['MassErrorVsMassData']
    else:
        errorVsTimeDataFilePath = ''    
        errorVsMassDataFilePath = ''
        
    # Confirm that the data files exist
    parentDirectoryPath = os.path.dirname(metadataFilePath)
    
    histogramDataFile   = ValidateFile(parentDirectoryPath, 'histogram', histogramDataFilePath)

    if not histogramDataFile:
        return
    
    if len(errorVsTimeDataFilePath) > 0:
        errorVsTimeDataFile = ValidateFile(parentDirectoryPath, 'mass error vs. time', errorVsTimeDataFilePath)
        errorVsMassDataFile = ValidateFile(parentDirectoryPath, 'mass error vs. mass', errorVsMassDataFilePath)

        if not errorVsTimeDataFile or not errorVsMassDataFile:
            return
    
    # Define the base output file path
    baseNameHead, baseNameTail = os.path.split(baseName)
    
    if baseNameTail == baseName:
        # Create the output files in same folder as the histogram data file
        histogramParentDirectoryPath = os.path.dirname(histogramDataFile)
        baseNamePath = os.path.join(histogramParentDirectoryPath, baseName)
    else:
        baseNamePath = baseName
    
    
    # Read the data files

    print('Reading ' + histogramDataFile)
    histogramData, histogramPlotLabels, histogramColumnOptions       = read_file(histogramDataFile)

    if len(errorVsTimeDataFilePath) > 0:
        print('Reading ' + errorVsTimeDataFile)
        print('Reading ' + errorVsMassDataFile)
        
        errorVsTimeData, errorVsTimePlotLabels, errorVsTimeColumnOptions = read_file(errorVsTimeDataFile)
        errorVsMassData, errorVsMassPlotLabels, errorVsMassColumnOptions = read_file(errorVsMassDataFile)
    
    histogramOutputFilePath  = baseNamePath + '_MZRefinery_Histograms.png'
            
    print('\nOutput: ' + histogramOutputFilePath)
    print()
    print('Plot "' + histogramData.columns[0] + '" vs. "' + histogramData.columns[1] + '"')            
    print("  {:,}".format(len(histogramData.index)) + ' data points')
    print()
    plot_histograms(histogramOutputFilePath, histogramData.columns, 
                    histogramData[histogramData.columns[0]], histogramData[histogramData.columns[1]], histogramData[histogramData.columns[2]], 
                    histogramPlotLabels['Title1'], histogramPlotLabels['Title2'])
    
    if len(errorVsTimeDataFilePath) == 0:
        return
       
    massErrorsOutputFilePath = baseNamePath + '_MZRefinery_MassErrors.png'

    print('\nOutput: ' + massErrorsOutputFilePath)
    print()
    print('Plot "' + errorVsTimeData.columns[0] + '" vs. "' + errorVsTimeData.columns[1] + '" and')
    print('Plot "' + errorVsMassData.columns[0] + '" vs. "' + errorVsMassData.columns[1] + '"')
    
    print("  {:,}".format(len(errorVsTimeData.index)) + ' data points')
    print("  {:,}".format(len(errorVsMassData.index)) + ' data points')
        
    print()
    plot_mass_errors(massErrorsOutputFilePath, errorVsTimeData.columns, errorVsMassData.columns,
                     errorVsTimeData[errorVsTimeData.columns[0]], errorVsTimeData[errorVsTimeData.columns[1]], errorVsTimeData[errorVsTimeData.columns[2]], 
                     errorVsMassData[errorVsMassData.columns[0]], errorVsMassData[errorVsMassData.columns[1]], errorVsMassData[errorVsMassData.columns[2]], 
                     errorVsTimePlotLabels['Title1'], errorVsTimePlotLabels['Title2'],
                     errorVsMassPlotLabels['Title1'], errorVsMassPlotLabels['Title2'])
    
    
def parse_metadata(plotOption):
    return {m.split('=')[0]:m.split('=')[1] for m in plotOption.split(';')}

def read_file(fpath):
    data = pd.read_csv(fpath, sep='\t', skiprows=2, header=0)
    with open(fpath, 'r') as f:
        # The first line has the plot title and axis labels
        plotLabelData = f.readline().split('[')[1].split(']')[0]
        plotLabels = parse_metadata(plotLabelData)

        # The second line has column options
        # These are semicolon separated key/value pairs for each column, with options for each column separated by a tab
        # At present, the code does not use these column options
        columnOptionData = f.readline().split('\t')
        columnOptions = [parse_metadata(colOption) for colOption in columnOptionData]

        return data, plotLabels, columnOptions

def update_ticks_and_axes_histograms(ax, plt, baseFontSize):
    update_ticks_and_axes(ax, plt, baseFontSize, '%.0f', False, '-', ':')

def update_ticks_and_axes_mass_errors(ax, plt, baseFontSize):
    update_ticks_and_axes(ax, plt, baseFontSize, '%.0f', True, ':', '-')

def update_ticks_and_axes(ax, plt, baseFontSize, yAxisFormatString, allowNegativeY, xAxisGridStyle, yAxisGridStyle):

    if not allowNegativeY:
        # Assure that the Y axis minimum is not negative
        ymin, ymax = plt.ylim()
    
        if ymin < 0:
            plt.ylim(ymin = 0)

    # plt.xticks(fontsize=baseFontSize-2)
    # plt.yticks(fontsize=baseFontSize-2)
    ax.yaxis.set_major_formatter(mtick.FormatStrFormatter(yAxisFormatString))
    ax.yaxis.set_minor_locator(mtick.AutoMinorLocator())
    
    # Optionally define the distance between tick labels
    # ax.xaxis.set_major_locator(mtick.MultipleLocator(5000))
    ax.xaxis.set_major_formatter(mtick.FuncFormatter(lambda x, p: format(int(x), ',')))
    ax.xaxis.set_minor_locator(mtick.AutoMinorLocator())

    xAxisGridlines = len(xAxisGridStyle) > 0
    yAxisGridlines = len(yAxisGridStyle) > 0
    
    if xAxisGridlines or yAxisGridlines:
        ax.grid(True)
        
        ax.xaxis.grid(xAxisGridlines, which='major', linestyle=xAxisGridStyle)
        ax.xaxis.grid(False, which='minor')
        
        ax.yaxis.grid(yAxisGridlines, which='major', linestyle=yAxisGridStyle)
        ax.yaxis.grid(False, which='minor')



def plot_histograms(outputFilePath, columnNames, mass_error_ppm, counts_original, counts_refined, title_original, title_refined):

    fig, (ax1, ax2) = plt.subplots(1, 2, sharey=False, figsize=(8.5333,5), dpi=120)

    ax1.plot(mass_error_ppm, counts_original, linewidth=1, color='blue')

    ax2.plot(mass_error_ppm, counts_refined, linewidth=1, color='green')

    xDataMin = np.min(mass_error_ppm)
    xDataMax = np.max(mass_error_ppm)
    
    # Assure that xDataMin to xDataMax spans at least -50 to 50
    if xDataMin >= -52 and xDataMin < 0:
        xDataMin = -50

    if xDataMax > 0 and xDataMax < 52:
        xDataMax = 50
    
    if len(counts_refined) == 0 or math.isnan(counts_refined[0]):
        print("Note: counts_refined has NaN values; the Refined Histogram will be blank")
        haveRefinedCounts = False
    else:
        haveRefinedCounts = True

    yDataMaxima = []
    yDataMaxima.append(np.max(counts_original))
    
    if haveRefinedCounts:
        yDataMaxima.append(np.max(counts_refined))
        
    yDataMax = np.max(yDataMaxima)

    yDataMax = yDataMax + 0.05 * yDataMax
    
    # X axis is mass error, in ppm
    xAxisLabel = columnNames[0]
    
    # Y axis is counts
    yAxisLabel = 'Counts'

    baseFontSize = 12

    # Axis labels are specified below    
    # plt.xlabel(xAxisLabel, fontsize=baseFontSize)
    # plt.ylabel(yAxisLabel, fontsize=baseFontSize)
#    pprint(vars(ax1.xaxis))

    update_ticks_and_axes_histograms(ax1, plt, baseFontSize)
    update_ticks_and_axes_histograms(ax2, plt, baseFontSize)

    ax1.set_title(title_original, fontsize=baseFontSize+2)
    ax2.set_title(title_refined, fontsize=baseFontSize+2)

    # Define a fixed X-axis range so that each subplot has the same X range
    ax1.set_xlim(xmin = xDataMin, xmax = xDataMax)
    ax2.set_xlim(xmin = xDataMin, xmax = xDataMax)

    ax1.set_ylim(ymin = 0, ymax = yDataMax)
    ax2.set_ylim(ymin = 0, ymax = yDataMax)
    
    ax1.xaxis.set_label_text(xAxisLabel, fontsize=baseFontSize)
    ax2.xaxis.set_label_text(xAxisLabel, fontsize=baseFontSize)

    ax1.yaxis.set_label_text(yAxisLabel, fontsize=baseFontSize)
    ax2.yaxis.set_label_text('')

    ax1.yaxis.set_ticks_position('left')
    ax1.yaxis.set_label_position('left')
        
    ax2.yaxis.set_ticks_position('left')
    
    # Could remove tick labels with:
    # ax2.set_yticklabels([])

    # Add a black line at X=0
    ax1.axvline(0, linestyle='-', color='k', linewidth=1.0)
    ax2.axvline(0, linestyle='-', color='k', linewidth=1.0)

    plt.tight_layout()

    plt.savefig(outputFilePath)
    
    print('Mass error histogram created')
    
    # Uncomment to view the plot with an interactive GUI
    #plt.show()


def plot_mass_errors(outputFilePath, errorVsTimeColumnNames, errorVsMassColumnNames, 
                     scan_times, scan_mass_errors_original, scan_mass_errors_refined,
                     mz_list, mz_mass_errors_original, mz_mass_errors_refined,
                     scan_title_original, scan_title_refined,
                     mz_title_original, mz_title_refined):
    
    fig, ((ax1, ax3), (ax2, ax4)) = plt.subplots(2, 2, sharex='col', sharey=False, figsize=(8.5,6.4), dpi=120)

    # Define the point size
    scale = 2
    
    ax1.scatter(scan_times, scan_mass_errors_original, s=scale, color='blue', alpha=0.4, label=scan_title_original)
    ax2.scatter(scan_times, scan_mass_errors_refined,  s=scale, color='blue', alpha=0.4, label=scan_title_refined)

    ax3.scatter(mz_list, mz_mass_errors_original, s=scale, color='green', alpha=0.4, label=mz_title_original)
    ax4.scatter(mz_list, mz_mass_errors_refined,  s=scale, color='green', alpha=0.4, label=mz_title_refined)
    
    scanTimeMin = np.min(scan_times)
    scanTimeMax = np.max(scan_times)
        
    if scanTimeMin < 0.5:
        scanTimeMin = 0

    scanTimeAxisPadding = 0.02 * (scanTimeMax - scanTimeMin)

    if scanTimeMin > 0:
        scanTimeMin -= scanTimeAxisPadding

    scanTimeMax += scanTimeAxisPadding
    
    mzMin = np.min(mz_list)
    mzMax = np.max(mz_list)
    
    mzAxisPadding = 0.02 * (mzMax - mzMin)

    mzMin -= mzAxisPadding
    mzMax += mzAxisPadding

    haveRefinedScanMassErrors = True
    haveRefinedMzMassErrors = True
            
    if len(scan_mass_errors_refined) == 0 or math.isnan(scan_mass_errors_refined[0]):
        print("Note: scan_mass_errors_refined has NaN values; the refined mass error vs. scan time plot will be blank")
        haveRefinedScanMassErrors = False

    if len(mz_mass_errors_refined) == 0 or math.isnan(mz_mass_errors_refined[0]):
        print("Note: mz_mass_errors_refined has NaN values; the refined mass error vs. mass plot will be blank")
        haveRefinedMzMassErrors = False
        
    yDataMinima = []
    yDataMinima.append(np.min(scan_mass_errors_original))
    yDataMinima.append(np.min(mz_mass_errors_original))

    if haveRefinedScanMassErrors:
        yDataMinima.append(np.min(scan_mass_errors_refined))

    if haveRefinedMzMassErrors:
        yDataMinima.append(np.min(mz_mass_errors_refined))

    yDataMin = np.min(yDataMinima)
    
    yDataMaxima = []
    yDataMaxima.append(np.max(scan_mass_errors_original))
    yDataMaxima.append(np.max(mz_mass_errors_original))

    if haveRefinedScanMassErrors:
        yDataMaxima.append(np.max(scan_mass_errors_refined))

    if haveRefinedMzMassErrors:
        yDataMaxima.append(np.max(mz_mass_errors_refined))
        
    yDataMax = np.max(yDataMaxima)
    
    # Assure that yDataMin to yDataMax spans at least -20 to 20
    if yDataMin >= -20 and yDataMin < 0:
        yDataMin = -20

    if yDataMax > 0 and yDataMax < 22:
        yDataMax = 20
                
    # X axis is either time (minutes) or mass (m/z)
    timeBasedXAxisLabel = errorVsTimeColumnNames[0]
    massBasedXAxisLabel = errorVsMassColumnNames[0]
    
    # Y axis is Mass Error, in ppm
    yAxisLabel = 'Mass Error (ppm)'

    baseFontSize = 12

    # Axis labels are specified below    
    # plt.xlabel(xAxisLabel, fontsize=baseFontSize)
    # plt.ylabel(yAxisLabel, fontsize=baseFontSize)
#    pprint(vars(ax1.xaxis))

    update_ticks_and_axes_mass_errors(ax1, plt, baseFontSize)
    update_ticks_and_axes_mass_errors(ax2, plt, baseFontSize)
    update_ticks_and_axes_mass_errors(ax3, plt, baseFontSize)
    update_ticks_and_axes_mass_errors(ax4, plt, baseFontSize)
        
    ax1.set_title(scan_title_original, fontsize=baseFontSize+2)
    ax2.set_title(scan_title_refined, fontsize=baseFontSize+2)
    ax3.set_title(mz_title_original, fontsize=baseFontSize+2)
    ax4.set_title(mz_title_refined, fontsize=baseFontSize+2)
    
    # Make sure the X-axis range is the same for paired plots
    ax1.set_xlim(xmin = scanTimeMin, xmax = scanTimeMax)
    ax2.set_xlim(xmin = scanTimeMin, xmax = scanTimeMax)
    
    ax3.set_xlim(xmin = mzMin, xmax = mzMax)
    ax4.set_xlim(xmin = mzMin, xmax = mzMax)
    
    # Define a fixed Y-axis range so that each subplot has the same Y range
    ax1.set_ylim(ymin = yDataMin, ymax = yDataMax)
    ax2.set_ylim(ymin = yDataMin, ymax = yDataMax)
    ax3.set_ylim(ymin = yDataMin, ymax = yDataMax)
    ax4.set_ylim(ymin = yDataMin, ymax = yDataMax)
    
    ax2.xaxis.set_label_text(timeBasedXAxisLabel, fontsize=baseFontSize)
    ax4.xaxis.set_label_text(massBasedXAxisLabel, fontsize=baseFontSize)

    ax1.yaxis.set_label_text(yAxisLabel, fontsize=baseFontSize)
    ax2.yaxis.set_label_text(yAxisLabel, fontsize=baseFontSize)

    #ax1.yaxis.set_ticks_position('left')
    #ax1.yaxis.set_label_position('left')
        
    #ax2.yaxis.set_ticks_position('right')    
    #ax2.yaxis.set_label_position('right')
    
    # ax3.yaxis.set_ticks_position('left')
    # ax3.yaxis.set_label_position('left')
        
    #ax4.yaxis.set_ticks_position('right')    
    #ax4.yaxis.set_label_position('right')
    
    # Add a black line at Y=0
    ax1.axhline(0, linestyle='-', color='k', linewidth=1.0)
    ax2.axhline(0, linestyle='-', color='k', linewidth=1.0)
    ax3.axhline(0, linestyle='-', color='k', linewidth=1.0)
    ax4.axhline(0, linestyle='-', color='k', linewidth=1.0)

    plt.tight_layout()

    plt.savefig(outputFilePath)
    
    print('Mass error trend plot created')
        

def plot_lc_mz(outputFilePath, columnNames, lc_scan_num, mz, intensities, title, r_label, l_label):
    plt = generate_heat_map(columnNames, lc_scan_num, mz, intensities, title, r_label, l_label, False)
    plt.savefig(outputFilePath)
    print('2D plot created')

def ShowExampleMetadataFile():
    print('The metadata file should have either 2 or 4 lines of information')
    print('Each line has a key/value pair of information, separated by an equals sign')
    print('Filenames can be simple filenames or full file paths')
    print()
    print('BaseOutputName=Output_File_Base_Name')
    print('HistogramData=Histogram_Data_File_Name')
    print('MassErrorVsTimeData=MassErrorsVsTime_Data_File_Name')
    print('MassErrorVsMassData=MassErrorsVsMass_Data_File_Name')

def ValidateFile(parentDirectoryPath, fileDescription, dataFilePath):
    dataFile = Path(dataFilePath)

    if dataFile.is_file():
        return dataFilePath

    altFilePath = os.path.join(parentDirectoryPath, os.path.basename(dataFilePath))
    altFile = Path(altFilePath)
    if altFile.is_file():
        return altFilePath

    print('\nError, ' + fileDescription + ' file not found: ' + dataFilePath)
    print('Also considered: ' + altFilePath)
    return False

import sys

if __name__ == '__main__':
    if len(sys.argv) != 2:
        print('\nError: please enter the file name to process (wildcards are supported)')
        exit()
    
    fileNameMatchSpec = sys.argv[1]

    filesProcessed = 0
    for metadataFile in glob.glob(fileNameMatchSpec):
        process_file(metadataFile)
        filesProcessed += 1
        
    if filesProcessed == 0:
        print('\nError: no files match:\n' + fileNameMatchSpec)


# Can list fonts with the following
#import matplotlib.font_manager
#from matplotlib.font_manager import findfont, FontProperties
#fonts = matplotlib.font_manager.findSystemFonts(fontpaths=None, fontext='ttf')
#for font in sorted(fonts):
    #print(font)

# View details on a specific font with
#font = findfont(FontProperties(family=['arial']))
#print (font)
#exit()
