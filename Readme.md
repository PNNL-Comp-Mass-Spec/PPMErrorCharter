# PPM Error Charter

## Overview

PPM Error Charter is a command line utility for generating plots of the 
mass measurement errors before and after processing with mzRefinery.

mzRefinery is a software tool for correcting systematic mass error biases in 
mass spectrometry data files. The software uses confident peptide spectrum matches 
from [MS-GF+](https://github.com/MSGFPlus/msgfplus) to evaluate three different calibration methods, 
then chooses the optimal transform function to remove systematic bias, typically resulting in 
a mass measurement error histogram centered at 0 ppm. MzRefinery is part of the 
ProteoWizard package (in the msconvert.exe tool) and it thus can read and write 
a wide variety of file formats.

Download ProteoWizard from https://proteowizard.sourceforge.io/

For more information on the algorithms employed by mzRefinery, see 
manuscript [Correcting systematic bias and instrument measurement drift with mzRefinery](https://pubmed.ncbi.nlm.nih.gov/26243018/)

### Requirements

On Windows, plots can be generated with either OxyPlot or Python.
On Linux, plots can only be generated with Python, and you must use PPMErrorCharterPython.exe with mono (see below)

To use Python, install Python 3.x (preferably Python 3.6 or newer) along with these three packages:
* `numpy` 
* `matplotlib`
* `pandas`

An example command to install these packages is
```
C:\ProgramData\Anaconda3\Scripts\pip.exe install numpy matplotlib pandas
```

On Windows, PPM Error Charter searches for `python.exe` by looking for subdirectories starting with "Python3" or "Python 3" below the following directories.
It also looks for `python.exe` within each of the following directories (e.g. `C:\ProgramData\Anaconda3\python.exe`)
* C:\Program Files
* C:\Program Files (x86)
* C:\Users\CurrentUser\AppData\Local\Programs
* C:\ProgramData\Anaconda3
* C:\

On Linux, python3 must be at `/usr/bin/python3`\
This is typically a symbolic link to the actual Python 3.x program

## Usage

The following is a typical workflow for using mzRefinery

1. Create a centroided .mzML file

```
MSConvert.exe C:\WorkDir\DatasetName_2016-09-28.raw --filter "peakPicking true 1-" --mzML --32  -o C:\WorkDir
```


2. Search the .mzML file using MS-GF+ and a fully tryptic search and no dynamic mods (do include alkylation of cysteine if applicable):

```
java.exe  -Xmx1500M -jar MSGFPlus.jar -s DatasetName_2016-09-28.mzML -o DatasetName_2016-09-28_msgfplus.mzid 
          -d C:\FASTA\ID_003456_9B916A8B.fasta  -t 50ppm -m 0 -inst 3 -e 1 -ti -1,2 -ntt 2 -tda 1 
          -minLength 6 -maxLength 50 -n 1 -thread 7 -mod MSGFDB_Mods.txt -minNumPeaks 5 -addFeatures 1
```

3. Run mzRefinery

```
msconvert.exe C:\WorkDir\DatasetName_2016-09-28.mzML --outfile DatasetName_2016-09-28_FIXED.mzML 
   --filter "mzRefiner C:\WorkDir\DatasetName_2016-09-28_msgfplus.mzid thresholdValue=-1e-10 thresholdStep=10 maxSteps=2" 
   --32 -mzML
```

4. Visualize the results from MzRefinery:

```
PPMErrorCharter.exe C:\WorkDir\DatasetName_2016-09-28_msgfplus.mzid 1E-10
```

On Linux, use PPMErrorCharterPython with [Mono](https://www.mono-project.com/download/stable/)
```
mono PPMErrorCharterPython.exe -I:C:\WorkDir\DatasetName_2016-09-28_msgfplus.mzid -Evalue:1E-10
```
 
5. Run MS-GF+ again, this time using a more thorough search, for example using partially tryptic or non-tryptic, or adding additional dynamic mods.

```
java.exe  -Xmx4000M -jar MSGFPlus.jar -s DatasetName_2016-09-28.mzML -o DatasetName_2016-09-28_msgfplus.mzid 
          -d C:\FASTA\ID_003456_9B916A8B.fasta  -t 10ppm -m 0 -inst 3 -e 1 -ti -1,1 -ntt 1 -tda 1 
          -minLength 6 -maxLength 50 -minCharge 2 -maxCharge 5 -n 1 -thread 8 -mod MSGFDB_Mods.txt -minNumPeaks 5 -addFeatures 1
```

```
java.exe  -Xmx2000M -XX:+UseConcMarkSweepGC -cp MSGFPlus.jar edu.ucsd.msjava.ui.MzIDToTsv 
          -i C:\WorkDir\DatasetName_2016-09-28_msgfplus.mzid -o C:\WorkDir\DatasetName_2016-09-28_msgfdb.tsv 
          -showQValue 1 -showDecoy 1 -unroll 1
```

## Syntax

Usage: PPMErrorCharter.exe or PPMErrorCharterPython.exe

`-I` (or the first non-switch argument)
* PSM results file; .mzid or .mzid.gz

`-EValue` or `-Threshold` (or the second non-switch argument)
* Spec EValue Threshold (Default: 1E-10, Min: 0, Max: 10)

`-F` or `-Fixed` or `-MzML`
* Path to the .mzML or .mzML.gz file with updated m/z values (created by MSConvert using the mzRefiner filter)
* If this switch is not used, PPM Error Charter will try to auto-find this file

`-O` or `-Output`
* Path to the directory where plots should be created
* By default, plots are created in the same directory as the input file

`-HP` or `-HistogramPlot`
* Histogram plot file path to use; overrides use of -O

`-MEP` or `-MassErrorPlot`
* Mass error plot file path to use; overrides use of -O

`-PPMBinSize` or `-Histogram`
* PPM mass error histogram bin size (Default: 0.5, Min: 0.1, Max: 10)

`-Python` or `-PythonPlot`
* Generate plots with Python
* Defaults to False for PPMErrorCharter.exe
* Always True for PPMErrorCharterPython.exe

`-Debug` or `-Verbose`
* Create a tab-delimited text file with detailed mass error information (Default: False)
* In addition, will not delete the _TmpExportData.txt files used to pass data to Python for plotting

## Example Commands

`PPMErrorCharter.exe SearchResults_msgfplus.mzid.gz`

`PPMErrorCharter.exe SearchResults_msgfplus.mzid.gz 1E-12`

`PPMErrorCharter.exe SearchResults_msgfplus.mzid.gz /F:C:\InstrumentFiles\SearchResults_msgfplus.mzML.gz 1E-12`

`PPMErrorCharter.exe SearchResults_msgfplus.mzid.gz /Python`

`PPMErrorCharter.exe SearchResults_msgfplus.mzid.gz /Python /Debug`

`PPMErrorCharterPython.exe -I:SearchResults_msgfplus.mzid.gz -EValue:1E-13`

## Contacts

Written by Bryson Gibbons and Matthew Monroe for the Department of Energy (PNNL, Richland, WA) \
E-mail: bryson.gibbons@pnnl.gov or proteomics@pnnl.gov \
Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics

## License

The PPM Error Charter is licensed under the 2-Clause BSD License; 
you may not use this program except in compliance with the License.
You may obtain a copy of the License at https://opensource.org/licenses/BSD-2-Clause

Copyright 2019 Battelle Memorial Institute
