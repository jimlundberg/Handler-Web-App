@ECHO OFF

REM Handler App test batch file to:
REM 1. Copies the job beginning files 75300037D00.xml and the .tab files
REM 2. Copies the mode csv files into the directory to simulate finishing job Input
REM 3. Waites until each job directory is created in the ProcessingBuffer (by Handler App)
REM 4. Then copies the files that complete a job into the ProcessingBuffer directory

:ConfigureTest

REM set buffer=ProcessingBuffer
set buffer=Input Buffer

REM set PassFail=Fail
set PassFail=Pass

set DELAY=1

REM Copy 5 directories into the Input Buffer to test waiting on the last one

:CopyDirectories

SET JOB=1185840_202003250942
echo Copying %JOB% .xml and .tab files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.tab" >nul
timeout %DELAY% >nul
echo Copying %JOB% mode*.csv files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.csv" >nul

timeout %DELAY% >nul

SET JOB=1185841_202005070801
echo Copying %JOB% .xml and .tab files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.tab" >nul
timeout %DELAY% >nul
echo Copying %JOB% mode*.csv files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.csv" >nul

timeout %DELAY% >nul

SET JOB=1202741_202003101418
echo Copying %JOB% .xml and .tab files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.tab" >nul
timeout %DELAY% >nul
echo Copying %JOB% mode*.csv files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.csv" >nul

timeout %DELAY% >nul

SET JOB=1278061_202006301109
echo Copying %JOB% .xml and .tab files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.tab" >nul
timeout %DELAY% >nul
echo Copying %JOB% mode*.csv files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.csv" >nul

timeout %DELAY% >nul

SET JOB=1178351_202005071438
echo Copying %JOB% .xml and .tab files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.tab" >nul
timeout %DELAY% >nul
echo Copying %JOB% mode*.csv files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.csv" >nul

timeout %DELAY% >nul

SET JOB=1178352_202005050818
echo Copying %JOB% .xml and .tab files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.tab" >nul
timeout %DELAY% >nul
echo Copying %JOB% mode*.csv files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.csv" >nul

timeout %DELAY% >nul

SET JOB=1307106_202002181307
echo Copying %JOB% .xml and .tab files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.tab" >nul
timeout %DELAY% >nul
echo Copying %JOB% mode*.csv files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.csv" >nul


timeout 30


:FillDirectories

:Fill_1

SET JOB="1185840_202003250942"
echo Scanning for ProcessingBuffer\%JOB% files
if not exist ProcessingBuffer\%JOB% GOTO Wait_1
copy "test\%JOB% - %PassFail%" ProcessingBuffer\%JOB% >nul
GOTO :Fill_2
:Wait_1
timeout 1 >nul
GOTO :Fill_1

:Fill_2
SET JOB="1185841_202005070801"
echo Scanning for ProcessingBuffer\%JOB% files
if not exist ProcessingBuffer\%JOB% GOTO Wait_2
copy "test\%JOB% - %PassFail%" ProcessingBuffer\%JOB% >nul
GOTO :Fill_3
:Wait_2
timeout 1 >nul
GOTO :Fill_2

:Fill_3
SET JOB="1202741_202003101418"
echo Scanning for ProcessingBuffer\%JOB% files
if not exist ProcessingBuffer\%JOB% GOTO Wait_3
copy "test\%JOB% - %PassFail%" ProcessingBuffer\%JOB% >nul
GOTO :Fill_4
:Wait_3
timeout 1 >nul
GOTO :Fill_3

:Fill_4
SET JOB="1278061_202006301109"
echo Scanning for ProcessingBuffer\%JOB% files
if not exist ProcessingBuffer\%JOB% GOTO Wait_4
copy "test\%JOB% - %PassFail%" ProcessingBuffer\%JOB% >nul
GOTO :Fill_5
:Wait_4
timeout 1 >nul
GOTO :Fill_4

:Fill_5
SET JOB="1178351_202005071438"
echo Scanning for ProcessingBuffer\%JOB% files
if not exist ProcessingBuffer\%JOB% GOTO Wait_5
copy "test\%JOB% - %PassFail%" ProcessingBuffer\%JOB% >nul
GOTO :Fill_6
:Wait_5
timeout 1 >nul
GOTO :Fill_5

:Fill_6
SET JOB="1178352_202005050818"
echo Scanning for ProcessingBuffer\%JOB% files
if not exist ProcessingBuffer\%JOB% GOTO Wait_6
copy "test\%JOB% - %PassFail%" ProcessingBuffer\%JOB% >nul
GOTO :Fill_7
:Wait_6
timeout 1 >nul
GOTO :Fill_6

:Fill_7
SET JOB="1307106_202002181307"
echo Scanning for ProcessingBuffer\%JOB% files
if not exist ProcessingBuffer\%JOB% GOTO Wait_7
copy "test\%JOB% - %PassFail%" ProcessingBuffer\%JOB% >nul
GOTO :End
:Wait_7
timeout 1 >nul
GOTO :Fill_7


:End