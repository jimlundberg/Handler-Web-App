@ECHO OFF

REM Handler App Partial Directory Add test
REM 1. First copies the job 75300037D00.xml file to the Input Buffer Job directory
REM 2. Then copies the job .csv and .tab files into the directory to simulate finishing job Input

:ConfigureTest

set buffer=Input Buffer
REM set buffer=ProcessingBuffer
set DirType=Pass
REM set DirType=Fail
set DELAY=10

REM Copy directories into the Input Buffer

SET JOB=1185840_202003250942
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%\*.xml" "%buffer%\%JOB%\*.xml"
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB%\*.csv" "%buffer%\%JOB%\*.csv"
xcopy /S /F "test\%JOB%\*.tab" "%buffer%\%JOB%\*.tab"

timeout 10

SET JOB=1307106_202002181307
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%\*.xml" "%buffer%\%JOB%\*.xml"
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB%\*.csv" "%buffer%\%JOB%\*.csv"
xcopy /S /F "test\%JOB%\*.tab" "%buffer%\%JOB%\*.tab"

timeout 10

SET JOB=1178350_202006030856
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%\*.xml" "%buffer%\%JOB%\*.xml"
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB%\*.csv" "%buffer%\%JOB%\*.csv"
xcopy /S /F "test\%JOB%\*.tab" "%buffer%\%JOB%\*.tab"

timeout 10

SET JOB=1202740_202006110832
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%\*.xml" "%buffer%\%JOB%\*.xml"
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB%\*.csv" "%buffer%\%JOB%\*.csv"
xcopy /S /F "test\%JOB%\*.tab" "%buffer%\%JOB%\*.tab"

timeout 10

SET JOB=1185841_202005070801
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%\*.xml" "%buffer%\%JOB%\*.xml"
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB%\*.csv" "%buffer%\%JOB%\*.csv"
xcopy /S /F "test\%JOB%\*.tab" "%buffer%\%JOB%\*.tab"

timeout 10

SET JOB=1202741_202003101418
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%\*.xml" "%buffer%\%JOB%\*.xml"
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB%\*.csv" "%buffer%\%JOB%\*.csv"
xcopy /S /F "test\%JOB%\*.tab" "%buffer%\%JOB%\*.tab"

timeout 10

SET JOB=1178352_202005050818
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%\*.xml" "%buffer%\%JOB%\*.xml"
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB%\*.csv" "%buffer%\%JOB%\*.csv"
xcopy /S /F "test\%JOB%\*.tab" "%buffer%\%JOB%\*.tab"

timeout 10

SET JOB=1278061_202006301109
echo Copying %JOB% .xml file
xcopy /S /F "test\%JOB%\*.xml" "%buffer%\%JOB%\*.xml"
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB%\*.csv" "%buffer%\%JOB%\*.csv"
xcopy /S /F "test\%JOB%\*.tab" "%buffer%\%JOB%\*.tab"

timeout 10

SET JOB=1610789_201911281057
xcopy /S /F "test\%JOB%\*.xml" "%buffer%\%JOB%\*.xml"
timeout 5
echo Copying %JOB% other files
xcopy /S /F "test\%JOB%\*.csv" "%buffer%\%JOB%\*.csv"
xcopy /S /F "test\%JOB%\*.tab" "%buffer%\%JOB%\*.tab"

:End