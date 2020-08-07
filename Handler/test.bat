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
set DELAY=5

REM Copy directories into the Input Buffer with only .xml file

:CreateDirectoriesWithOnlyXml

SET JOB=1307106_202002181307
echo Copying %JOB% .xml file
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul

timeout %DELAY% >nul

SET JOB=1278061_202006301109
echo Copying %JOB% .xml file
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul

timeout %DELAY% >nul

SET JOB=1202741_202003101418
echo Copying %JOB% .xml file
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul

timeout %DELAY% >nul

SET JOB=1185841_202005070801
echo Copying %JOB% .xml file
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul

timeout %DELAY% >nul

SET JOB=1185840_202003250942
echo Copying %JOB% .xml file
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul

timeout %DELAY% >nul

SET JOB=1178352_202005050818
echo Copying %JOB% .xml file
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul

timeout %DELAY% >nul

:CompleteDirectoryJobs

SET JOB=1307106_202002181307
echo Copying %JOB% .tab and .csv files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.tab" >nul
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.csv" >nul

timeout %DELAY% >nul

SET JOB=1278061_202006301109
echo Copying %JOB% .tab and .csv files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.tab" >nul
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.csv" >nul

timeout %DELAY% >nul

SET JOB=1202741_202003101418
echo Copying %JOB% .tab and .csv files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.tab" >nul
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.csv" >nul

timeout %DELAY% >nul

SET JOB=1185841_202005070801
echo Copying %JOB% .tab and .csv files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.tab" >nul
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.csv" >nul

timeout %DELAY% >nul

SET JOB=1185840_202003250942
echo Copying %JOB% .tab and .csv files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.tab" >nul
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.csv" >nul

timeout %DELAY% >nul

SET JOB=1178352_202005050818
echo Copying %JOB% .tab and .csv files
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.tab" >nul
robocopy /NFL /NDL /NJH /NJS "test\%JOB%" "%buffer%\%JOB%" "*.csv" >nul


GOTO End
timeout 30


:FillProcessingBufferDirectories


:Fill_1
if EXIST ProcessingBuffer\1278061_202006301109 (
echo ProcessingBuffer\1278061_202006301109 exists
copy "test\1278061_202006301109 - Pass" ProcessingBuffer\1278061_202006301109
) ELSE (
echo ProcessingBuffer\1278061_202006301109 not created yet
timeout %DELAY% >nul
GOTO Fill_2
)

:Fill_2
if EXIST ProcessingBuffer\1202741_202003101418 (
echo ProcessingBuffer\1202741_202003101418 exists
copy "test\1202741_202003101418 - Pass" ProcessingBuffer\1202741_202003101418
) ELSE (
echo ProcessingBuffer\1202741_202003101418 not created yet
timeout %DELAY% >nul
GOTO Fill_3
)

:Fill_3
if EXIST ProcessingBuffer\1185841_202005070801 (
echo ProcessingBuffer\1185841_202005070801 exists
copy "test\1185841_202005070801 - Pass" ProcessingBuffer\1185841_202005070801
) ELSE (
echo ProcessingBuffer\1185841_202005070801 not created yet
timeout %DELAY% >nul
GOTO Fill_4
)

:Fill_4
if EXIST ProcessingBuffer\1307106_202002181307 (
echo ProcessingBuffer\1307106_202002181307 exists
copy "test\1307106_202002181307 - Pass" ProcessingBuffer\1307106_202002181307
) ELSE (
echo ProcessingBuffer\1307106_202002181307 not created yet
timeout %DELAY% >nul
GOTO Fill_5
)

:Fill_5
if EXIST ProcessingBuffer\1178352_202005050818 (
echo ProcessingBuffer\1178352_202005050818 exists
copy "test\1178352_202005050818 - Pass" ProcessingBuffer\1178352_202005050818
) ELSE (
echo ProcessingBuffer\1178352_202005050818 not created yet
timeout %DELAY% >nul
GOTO Fill_6
)

:Fill_6
if EXIST ProcessingBuffer\1185840_202003250942 (
echo ProcessingBuffer\1185840_202003250942 exists
copy "test\1185840_202003250942 - Pass" ProcessingBuffer\1185840_202003250942
) ELSE (
echo ProcessingBuffer\1185840_202003250942 not created yet
timeout %DELAY% >nul
echo.
GOTO Fill_1
)


:End
