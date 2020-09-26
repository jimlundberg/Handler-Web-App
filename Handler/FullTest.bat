@echo off

REM Drops a new job directory into the Input Buffer every 10 seconds

:ConfigureTest

set buffer=Input Buffer
REM set buffer=ProcessingBuffer
set DirType=Pass
REM set DirType=Fail

:RunTest

SET JOB=1185840_202003250942
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"
xcopy /S /I /Q "test\%JOB% - Start" "%buffer%\%JOB%"

timeout 10

SET JOB=1307106_202002181307
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"
xcopy /S /I /Q "test\%JOB% - Start" "%buffer%\%JOB%"

timeout 10

SET JOB=1178350_202006030856
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"
xcopy /S /I /Q "test\%JOB% - Start" "%buffer%\%JOB%"

timeout 10

SET JOB=1202740_202006110832
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"
xcopy /S /I /Q "test\%JOB% - Start" "%buffer%\%JOB%"

timeout 10

SET JOB=1185841_202005070801
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"
xcopy /S /I /Q "test\%JOB% - Start" "%buffer%\%JOB%"

timeout 10

SET JOB=1202741_202003101418
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"
xcopy /S /I /Q "test\%JOB% - Start" "%buffer%\%JOB%"

timeout 10

SET JOB=1178352_202005050818
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"
xcopy /S /I /Q "test\%JOB% - Start" "%buffer%\%JOB%"

timeout 10

SET JOB=1278061_202006301109
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"
xcopy /S /I /Q "test\%JOB% - Start" "%buffer%\%JOB%"

timeout 10

SET JOB=1610789_201911281057
echo Copying %JOB%
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"
xcopy /S /I /Q "test\%JOB% - Start" "%buffer%\%JOB%"

:End