# PPMErrorCharter

## Overview
PPMErrorCharter is a command line utility for generating plots of the 
mass measurement errors before and after processing with mzRefinery.

mzRefinery is a software tool for correcting systematic mass error biases in 
mass spectrometry data files. The software uses confident peptide spectrum matches 
from MSGF+ to evaluate three different calibration methods, then chooses the 
optimal transform function to remove systematic bias, typically resulting in 
a mass measurement error histogram centered at 0 ppm. MzRefinery is part of the 
ProteoWizard package (in the msconvert.exe tool) and it thus can read and write 
a wide variety of file formats.

Download ProteoWizard from http://proteowizard.sourceforge.net/

For more information on the algorithms employed by mzRefinery, see also http://www.ncbi.nlm.nih.gov/pubmed/26243018

## Usage

The following is a typical workflow for using mzRefinery

1. Create a centroided .mzML file

`MSConvert.exe C:\WorkDir\DatasetName_2016-09-28.raw --filter "peakPicking true 1-" --mzML --32  -o C:\WorkDir`


2. Search the .mzML file using MSGF+ and a fully tryptic search and no dynamic mods (do include alkylation of cysteine if applicable):

```
java.exe  -Xmx1500M -jar MSGFPlus.jar -s DatasetName_2016-09-28.mzML -o DatasetName_2016-09-28_msgfplus.mzid 
          -d C:\FASTA\ID_003456_9B916A8B.fasta  -t 50ppm -m 0 -inst 3 -e 1 -ti -1,2 -ntt 2 -tda 1 
          -minLength 6 -maxLength 50 -n 1 -thread 7 -mod MSGFDB_Mods.txt -minNumPeaks 5 -addFeatures 1
```

3. Run mzRefinery

```
msconvert.exe C:\WorkDir\DatasetName_2016-09-28.mzML --outfile DatasetName_2016-09-28_FIXED.mzML 
   --filter "mzRefiner C:\WorkDir\DatasetName_2016-09-28_msgfplus.mzid thresholdValue=-1e-10 thresholdStep=10 maxSteps=2" 
   --32 –mzML
 ```

4. Visualize the results from MzRefinery:

`PPMErrorCharter.exe C:\WorkDir\DatasetName_2016-09-28_msgfplus.mzid 1E-10`
 

5. Run MSGF+ again, this time using a more thorough search, for example using partially tryptic or non-tryptic, or adding additional dynamic mods.

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

Usage: PPMErrorCharter.exe

`-I` (or the first non-switch argument)
* PSM results file; mzid or .mzid.gz (Default: "")

`-EValue` or `-Threshold` (or the second non-switch argument)
* Spec EValue Threshold (Default: 1E-10, Min: 0, Max: 10)

`-PPMBinSize` or `-Histogram`
* PPM mass error histogram bin size (Default: 0.5, Min: 0.1, Max: 10)

`-Python` or `-PythonPlot`
Generate plots with Python (Default: False)

`-Debug` or `-Verbose`
* Create a tab-delimited text file with detailed mass error information (Default: False)

## Example Commands

`PPMErrorCharter.exeSearchResults_msgfplus.mzid.gz`

`PPMErrorCharter.exeSearchResults_msgfplus.mzid.gz 1E-12`

`PPMErrorCharter.exeSearchResults_msgfplus.mzid.gz /Python`

`PPMErrorCharter.exeSearchResults_msgfplus.mzid.gz /Python /Debug`

## Contacts

Written by Bryson Gibbons and Matthew Monroe for the Department of Energy (PNNL, Richland, WA) \
E-mail: bryson.gibbons@pnnl.gov or proteomics@pnnl.gov \
Website: https://panomics.pnl.gov/ or https://omics.pnl.gov

## License

The PPMErrorCharter is licensed under the 2-Clause BSD License; 
you may not use this file except in compliance with the License.
You may obtain a copy of the License at https://opensource.org/licenses/BSD-2-Clause

Copyright 2018 Battelle Memorial Institute
