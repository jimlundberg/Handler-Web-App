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
echo Copying Job %JOB% files one at a time
SlowCopy "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\75300037D00.xml"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode1.csv" "%buffer%\%JOB%\%JOB%_mode1.csv"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode2.csv" "%buffer%\%JOB%\%JOB%_mode2.csv"
timeout 5
SlowCopy "test\%JOB%\Cap_Template.tab" "%buffer%\%JOB%\Cap_Template.tab"
timeout 5
SlowCopy "test\%JOB%\Tune_Template.tab" "%buffer%\%JOB%\Tune_Template.tab"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode0.csv" "%buffer%\%JOB%\%JOB%_mode0.csv"

timeout 10

SET JOB=1307106_202002181307
echo Copying Job %JOB% files one at a time
SlowCopy "test\%JOB%\%JOB%_mode1.csv" "%buffer%\%JOB%\%JOB%_mode1.csv"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode2.csv" "%buffer%\%JOB%\%JOB%_mode2.csv"
timeout 5
SlowCopy "test\%JOB%\Cap_Template.tab" "%buffer%\%JOB%\Cap_Template.tab"
timeout 5
SlowCopy "test\%JOB%\Tune_Template.tab" "%buffer%\%JOB%\Tune_Template.tab"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode0.csv" "%buffer%\%JOB%\%JOB%_mode0.csv"
timeout 5
SlowCopy "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\75300037D00.xml"

timeout 10

SET JOB=1178350_202006030856
echo Copying Job %JOB% files one at a time
SlowCopy "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\75300037D00.xml"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode1.csv" "%buffer%\%JOB%\%JOB%_mode1.csv"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode2.csv" "%buffer%\%JOB%\%JOB%_mode2.csv"
timeout 5
SlowCopy "test\%JOB%\Cap_Template.tab" "%buffer%\%JOB%\Cap_Template.tab"
timeout 5
SlowCopy "test\%JOB%\Tune_Template.tab" "%buffer%\%JOB%\Tune_Template.tab"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode0.csv" "%buffer%\%JOB%\%JOB%_mode0.csv"

timeout 10

SET JOB=1202740_202006110832
echo Copying Job %JOB% files one at a time
SlowCopy "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\75300037D00.xml"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode1.csv" "%buffer%\%JOB%\%JOB%_mode1.csv"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode2.csv" "%buffer%\%JOB%\%JOB%_mode2.csv"
timeout 5
SlowCopy "test\%JOB%\Cap_Template.tab" "%buffer%\%JOB%\Cap_Template.tab"
timeout 5
SlowCopy "test\%JOB%\Tune_Template.tab" "%buffer%\%JOB%\Tune_Template.tab"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode0.csv" "%buffer%\%JOB%\%JOB%_mode0.csv"

timeout 10

SET JOB=1185841_202005070801
echo Copying Job %JOB% files one at a time
SlowCopy "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\75300037D00.xml"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode1.csv" "%buffer%\%JOB%\%JOB%_mode1.csv"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode2.csv" "%buffer%\%JOB%\%JOB%_mode2.csv"
timeout 5
SlowCopy "test\%JOB%\Cap_Template.tab" "%buffer%\%JOB%\Cap_Template.tab"
timeout 5
SlowCopy "test\%JOB%\Tune_Template.tab" "%buffer%\%JOB%\Tune_Template.tab"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode0.csv" "%buffer%\%JOB%\%JOB%_mode0.csv"

timeout 10

SET JOB=1202741_202003101418
echo Copying Job %JOB% files one at a time
SlowCopy "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\75300037D00.xml"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode1.csv" "%buffer%\%JOB%\%JOB%_mode1.csv"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode2.csv" "%buffer%\%JOB%\%JOB%_mode2.csv"
timeout 5
SlowCopy "test\%JOB%\Cap_Template.tab" "%buffer%\%JOB%\Cap_Template.tab"
timeout 5
SlowCopy "test\%JOB%\Tune_Template.tab" "%buffer%\%JOB%\Tune_Template.tab"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode0.csv" "%buffer%\%JOB%\%JOB%_mode0.csv"

timeout 10

SET JOB=1178352_202005050818
echo Copying Job %JOB% files one at a time
SlowCopy "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\75300037D00.xml"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode1.csv" "%buffer%\%JOB%\%JOB%_mode1.csv"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode2.csv" "%buffer%\%JOB%\%JOB%_mode2.csv"
timeout 5
SlowCopy "test\%JOB%\Cap_Template.tab" "%buffer%\%JOB%\Cap_Template.tab"
timeout 5
SlowCopy "test\%JOB%\Tune_Template.tab" "%buffer%\%JOB%\Tune_Template.tab"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode0.csv" "%buffer%\%JOB%\%JOB%_mode0.csv"

timeout 10

SET JOB=1278061_202006301109
echo Copying Job %JOB% files one at a time
SlowCopy "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\75300037D00.xml"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode1.csv" "%buffer%\%JOB%\%JOB%_mode1.csv"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode2.csv" "%buffer%\%JOB%\%JOB%_mode2.csv"
timeout 5
SlowCopy "test\%JOB%\Cap_Template.tab" "%buffer%\%JOB%\Cap_Template.tab"
timeout 5
SlowCopy "test\%JOB%\Tune_Template.tab" "%buffer%\%JOB%\Tune_Template.tab"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode0.csv" "%buffer%\%JOB%\%JOB%_mode0.csv"

timeout 10

SET JOB=1610789_201911281057
echo Copying Job %JOB% files one at a time
SlowCopy "test\%JOB%\75300037D00.xml" "%buffer%\%JOB%\75300037D00.xml"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode1.csv" "%buffer%\%JOB%\%JOB%_mode1.csv"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode2.csv" "%buffer%\%JOB%\%JOB%_mode2.csv"
timeout 5
SlowCopy "test\%JOB%\Cap_Template.tab" "%buffer%\%JOB%\Cap_Template.tab"
timeout 5
SlowCopy "test\%JOB%\Tune_Template.tab" "%buffer%\%JOB%\Tune_Template.tab"
timeout 5
SlowCopy "test\%JOB%\%JOB%_mode0.csv" "%buffer%\%JOB%\%JOB%_mode0.csv"

:End