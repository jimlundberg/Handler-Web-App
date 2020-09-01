@ECHO OFF

REM Handler App test batch file to:
REM 1. Copies the job 75300037D00.xml file with directory into the Input Buffer
REM 2. Copies the job .csv and .tab files into the directory to stimulate job start

:ConfigureTest

set buffer=Input Buffer
REM set buffer=ProcessingBuffer
set DirType=Pass
REM set DirType=Fail

:RunTest

SET JOB=1185840_202003250942
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB% - Start" "%buffer%\%JOB%" "*.*" >nul

timeout 10

SET JOB=1307106_202002181307
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB% - Start" "%buffer%\%JOB%" "*.*" >nul

timeout 10

SET JOB=1178350_202006030856
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB% - Start" "%buffer%\%JOB%" "*.*" >nul

timeout 10

SET JOB=1202740_202006110832
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB% - Start" "%buffer%\%JOB%" "*.*" >nul

timeout 10

SET JOB=1185841_202005070801
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB% - Start" "%buffer%\%JOB%" "*.*" >nul

timeout 10

SET JOB=1202741_202003101418
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB% - Start" "%buffer%\%JOB%" "*.*" >nul

timeout 10

SET JOB=1178352_202005050818
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB% - Start" "%buffer%\%JOB%" "*.*" >nul

timeout 10

SET JOB=1278061_202006301109
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB% - Start" "%buffer%\%JOB%" "*.*" >nul

timeout 10

SET JOB=1610789_201911281057
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%" "%buffer%\%JOB%" "*.xml" >nul
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB% - Start" "%buffer%\%JOB%" "*.*" >nul

:End