@ECHO OFF

REM Handler App test batch file to:
REM 1. Copies the job 75300037D00.xml files
REM 2. Copies the job .csv and .tab files into the directory to simulate finishing job Input
REM 3. Waits until each job directory is created in the ProcessingBuffer (by Handler App)
REM 4. Then copies the files that complete a job into the ProcessingBuffer directory

:ConfigureTest

set buffer=Input Buffer
REM set buffer=ProcessingBuffer
set DirType=Pass
REM set DirType=Fail
set DELAY=10

REM Copy directories into the Input Buffer

SET JOB=1185840_202003250942
echo Copying %JOB% .xml file
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
timeout 5
echo Copying %JOB% other files
robocopy /NFL /NDL /NJH /NJS "test\%JOB% - Start" "%buffer%\%JOB%" "*.*" >nul

timeout 10

SET JOB=1307106_202002181307
echo Copying %JOB% .xml file
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
timeout 5
echo Copying %JOB% other files
robocopy /NFL /NDL /NJH /NJS "test\%JOB% - Start" "%buffer%\%JOB%" "*.*" >nul

timeout 10

SET JOB=1278061_202006301109
echo Copying %JOB% .xml file
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
timeout 5
echo Copying %JOB% other files
robocopy /NFL /NDL /NJH /NJS "test\%JOB% - Start" "%buffer%\%JOB%" "*.*" >nul

timeout 10

SET JOB=1202741_202003101418
echo Copying %JOB% .xml file
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
timeout 5
echo Copying %JOB% other files
robocopy /NFL /NDL /NJH /NJS "test\%JOB% - Start" "%buffer%\%JOB%" "*.*" >nul

timeout 10

SET JOB=1185841_202005070801
echo Copying %JOB% .xml file
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
timeout 5
echo Copying %JOB% other files
robocopy /NFL /NDL /NJH /NJS "test\%JOB% - Start" "%buffer%\%JOB%" "*.*" >nul

timeout 10

SET JOB=1202741_202003101418
echo Copying %JOB% .xml file
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
timeout 5
echo Copying %JOB% other files
robocopy /NFL /NDL /NJH /NJS "test\%JOB% - Start" "%buffer%\%JOB%" "*.*" >nul

:End