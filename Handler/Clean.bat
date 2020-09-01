@echo off

:ClearWorkingDirectories

cd "ProcessingBuffer"
del /F /Q /S *.* >nul 2>&1
rd /s /q . >nul 2>&1
cd..

cd "Archive"
del /F /Q /S *. >nul 2>&1
rd /s /q . >nul 2>&1
cd..

cd "Input Buffer"
del /F /Q /S *. >nul 2>&1
rd /s /q . >nul 2>&1
cd..

cd "Output Buffer"
del /F /Q /S *. >nul 2>&1
rd /s /q . >nul 2>&1
cd..

cd "Error Buffer"
del /F /Q /S *. >nul 2>&1
rd /s /q . >nul 2>&1
cd..

:DeleteLogFiles

del /F /Q /S Handler\log.txt >nul 2>&1
del /F /Q /S Handler\StatusData.csv >nul 2>&1

cls

:End