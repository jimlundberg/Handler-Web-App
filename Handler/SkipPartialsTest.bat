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
xcopy /F /R /Y /I "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\"
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode1.csv" "%buffer%\%JOB%\"
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode2.csv" "%buffer%\%JOB%\"
xcopy /F /R /Y /I "test\%JOB% - Start\Tune_Template.tab" "%buffer%\%JOB%\"

SET JOB=1178350_202006030856
echo Copying some %JOB% Job files
xcopy /F /R /Y /I  "test\%JOB% - Start\%JOB%_mode1.csv" "%buffer%\%JOB%\"
xcopy /F /R /Y /I  "test\%JOB% - Start\Tune_Template.tab" "%buffer%\%JOB%\"
xcopy /F /R /Y /I  "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\"
xcopy /F /R /Y /I  "test\%JOB% - Start\%JOB%_mode0.csv" "%buffer%\%JOB%\"

SET JOB=1185841_202005070801
echo Copying some %JOB% Job files
xcopy /F /R /Y /I  "test\%JOB% - Start\%JOB%_mode1.csv" "%buffer%\%JOB%\"
xcopy /F /R /Y /I  "test\%JOB% - Start\%JOB%_mode2.csv" "%buffer%\%JOB%\"
xcopy /F /R /Y /I  "test\%JOB% - Start\Cap_Template.tab" "%buffer%\%JOB%\"
xcopy /F /R /Y /I  "test\%JOB% - Start\Tune_Template.tab" "%buffer%\%JOB%\"
xcopy /F /R /Y /I  "test\%JOB% - Start\%JOB%_mode0.csv" "%buffer%\%JOB%\"

SET JOB=1178352_202005050818
echo Copying ALL %JOB% Job files
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"

timeout 30

SET JOB=1178350_202006030856
echo Copying the rest of %JOB% Job files
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode2.csv" "%buffer%\%JOB%\"
xcopy /F /R /Y /I  "test\%JOB% - Start\Cap_Template.tab" "%buffer%\%JOB%\"

timeout 10

SET JOB=1307106_202002181307
echo Copying the rest of %JOB% Job files
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode0.csv" "%buffer%\%JOB%\"
xcopy /F /R /Y /I  "test\%JOB% - Start\Cap_Template.tab" "%buffer%\%JOB%\"

timeout 10

SET JOB=1185841_202005070801
echo Copying the rest of %JOB% Job files
xcopy /F /R /Y /I  "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\"

:End