:ClearWorkingDirectories

@ECHO OFF

cd "ProcessingBuffer"
del /F /Q /S *.* >nul 2>&1
rd /Q /S . >nul 2>&1
cd..

cd "Archive"
del /F /Q /S *. >nul 2>&1
rd /Q /S . >nul 2>&1
cd..

cd "Input Buffer"
del /F /Q /S *. >nul 2>&1
rd /Q /S . >nul 2>&1
cd..

cd "Output Buffer"
del /F /Q /S *. >nul 2>&1
rd /Q /S . >nul 2>&1
cd..

cd "Error Buffer"
del /F /Q /S *. >nul 2>&1
rd /Q /S . >nul 2>&1
cd..

cls
