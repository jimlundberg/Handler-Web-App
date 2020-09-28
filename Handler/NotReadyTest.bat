@ECHO OFF

REM Create test setup with 5 jobs and 3 of them partial to start

:ConfigureTest

set buffer=Input Buffer
REM set buffer=ProcessingBuffer

:RunTest

SET JOB=1185840_202003250942
echo Copying ALL %JOB% Job files
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"

SET JOB=1307106_202002181307
echo Copying some %JOB% Job files
SlowCopy "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\75300037D00.xml"
SlowCopy "test\%JOB% - Start\%JOB%_mode1.csv" "%buffer%\%JOB%\%JOB%_mode1.csv"
SlowCopy "test\%JOB% - Start\%JOB%_mode2.csv" "%buffer%\%JOB%\%JOB%_mode2.csv"
SlowCopy "test\%JOB% - Start\Tune_Template.tab" "%buffer%\%JOB%\Tune_Template.tab"

SET JOB=1178350_202006030856
echo Copying some %JOB% Job files
SlowCopy "test\%JOB% - Start\%JOB%_mode1.csv" "%buffer%\%JOB%\%JOB%_mode1.csv"
SlowCopy "test\%JOB% - Start\Tune_Template.tab" "%buffer%\%JOB%\Tune_Template.tab"
SlowCopy "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\75300037D00.xml"
SlowCopy "test\%JOB% - Start\%JOB%_mode0.csv" "%buffer%\%JOB%\%JOB%_mode0.csv"

SET JOB=1185841_202005070801
echo Copying some %JOB% Job files
SlowCopy "test\%JOB% - Start\%JOB%_mode1.csv" "%buffer%\%JOB%\%JOB%_mode1.csv"
SlowCopy "test\%JOB% - Start\%JOB%_mode2.csv" "%buffer%\%JOB%\%JOB%_mode2.csv"
SlowCopy "test\%JOB% - Start\Cap_Template.tab" "%buffer%\%JOB%\Cap_Template.tab"
SlowCopy "test\%JOB% - Start\Tune_Template.tab" "%buffer%\%JOB%\Tune_Template.tab"
SlowCopy "test\%JOB% - Start\%JOB%_mode0.csv" "%buffer%\%JOB%\%JOB%_mode0.csv"

SET JOB=1178352_202005050818
echo Copying ALL %JOB% Job files
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"

:End