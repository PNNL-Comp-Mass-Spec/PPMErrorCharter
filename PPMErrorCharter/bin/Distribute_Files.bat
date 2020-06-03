xcopy Debug\PPMErrorCharter.exe C:\DMS_Programs\MzRefinery\ /D /Y
xcopy Debug\PPMErrorCharter.pdb C:\DMS_Programs\MzRefinery\ /D /Y
xcopy Debug\*.dll C:\DMS_Programs\MzRefinery\ /D /Y

@echo off
echo.
echo.
echo About to copy to AnalysisToolManagerDistribution
echo.
pause
@echo on

xcopy Debug\PPMErrorCharter.exe \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MzRefinery\ /D /Y
xcopy Debug\PPMErrorCharter.pdb \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MzRefinery\ /D /Y
xcopy Debug\*.dll \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MzRefinery\ /D /Y

pause
