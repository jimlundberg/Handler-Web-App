@ECHO OFF

REM Handler App test batch file to:
REM 1. Copies the job 75300037D00.xml files from 
REM 2. Copies the job .csv and .tab files into the directory to simulate finishing job Input
REM 3. Waits until each job directory is created in the ProcessingBuffer (by Handler App)
REM 4. Then copies the files that complete a job into the ProcessingBuffer directory

:ConfigureTest

set buffer=Input Buffer
REM set buffer=ProcessingBuffer
set DirType=Pass
REM set DirType=Fail

:RunTest

SET JOB=1185840_202003250942
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "ProcessingBuffer\%JOB%"

timeout 10

SET JOB=1307106_202002181307
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "ProcessingBuffer\%JOB%"

timeout 10

SET JOB=1178350_202006030856
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "ProcessingBuffer\%JOB%"

timeout 10

SET JOB=1202740_202006110832
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"

timeout 10

SET JOB=1185841_202005070801
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"

timeout 10

SET JOB=1202741_202003101418
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"

timeout 10

SET JOB=1178352_202005050818
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"

timeout 10

SET JOB=1278061_202006301109
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"

timeout 10

SET JOB=1610789_201911281057
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"

:End