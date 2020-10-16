@ECHO OFF

REM Handler App Both Partial and Full Directory type adds test
REM 1. Copies some the job 75300037D00.xml file to the Input Buffer Job directory
REM 2. Then copies the job .csv and .tab files into the directory to simulate finishing job Input
REM 3. Interspaces Full directory addes between Partial adds

:ConfigureTest

set buffer=\\ftcsrv01\ENG_GROUPS\TESTDATA\Navigator\NavII_FastCap\ModelerData\Input Buffer

:RunTest

SET JOB=1185840_202003250942
echo Copying %JOB% Complete directory
xcopy /S /I /Q "test\%JOB%" "%buffer%\%JOB%"

timeout 10

SET JOB=1307106_202002181307
echo Copying Job %JOB% files one at a time
xcopy /F /R /Y /I "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode1.csv" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode2.csv" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\Tune_Template.tab" "%buffer%\%JOB%\"

timeout 5

SET JOB=1178350_202006030856
echo Copying Job %JOB% files one at a time
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode1.csv" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode2.csv" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\Cap_Template.tab" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\Tune_Template.tab" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode0.csv" "%buffer%\%JOB%\"

timeout 5

SET JOB=1307106_202002181307
echo Copying the rest of Job %JOB% files one at a time
xcopy /F /R /Y /I "test\%JOB% - Start\Cap_Template.tab" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode0.csv" "%buffer%\%JOB%\"

timeout 5

SET JOB=1178350_202006030856
echo Copying the rest of Job %JOB% files one at a time
xcopy /F /R /Y /I "test\%JOB% - Start\Tune_Template.tab" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\"

timeout 10

SET JOB=1202740_202006110832
echo Copying %JOB% Complete directory
xcopy /F /R /Y /I "test\%JOB%" "%buffer%\%JOB%"

timeout 10

SET JOB=1185841_202005070801
echo Copying %JOB% Job files one at a time
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode1.csv" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode2.csv" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\Cap_Template.tab" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\Tune_Template.tab" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode0.csv" "%buffer%\%JOB%\"
timeout 10

SET JOB=1202741_202003101418
echo Copying %JOB% Complete directory
xcopy /F /R /Y /I "test\%JOB%" "%buffer%\%JOB%"

timeout 10

SET JOB=1178352_202005050818
echo Copying %JOB% Job files one at a time
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode1.csv" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode2.csv" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\Cap_Template.tab" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\Tune_Template.tab" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode0.csv" "%buffer%\%JOB%\"

timeout 10

SET JOB=1278061_202006301109
echo Copying %JOB% Job files one at a time
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode1.csv" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode2.csv" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\Cap_Template.tab" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\Tune_Template.tab" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB% - Start\%JOB%_mode0.csv" "%buffer%\%JOB%\"
timeout 5
xcopy /F /R /Y /I "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\"

timeout 10

SET JOB=1610789_201911281057
echo Copying %JOB% Complete directory
xcopy /F /R /Y /I "test\%JOB%" "%buffer%\%JOB%"

:End