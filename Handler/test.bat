REM Copy directories into the Input Buffer

REM set buffer=ProcessBuffer
set buffer=Input Buffer
set PassFail=Pass

xcopy /S /I /E "test\1185840_202003250942" "%buffer%\1185840_202003250942"
powershell -command "Start-Sleep -s 2"

xcopy /S /I /E "test\1185841_202005070801" "%buffer%\1185841_202005070801"
powershell -command "Start-Sleep -s 2"

xcopy /S /I /E "test\1202741_202003101418" "%buffer%\1202741_202003101418"
powershell -command "Start-Sleep -s 2"

xcopy /S /I /E "test\1278061_202006301109" "%buffer%\1278061_202006301109"
powershell -command "Start-Sleep -s 2"

xcopy /S /I /E "test\1658348_202003111530" "%buffer%\1658348_202003111530"
powershell -command "Start-Sleep -s 2"

xcopy /S /I /E "test\1658349_202003131343" "%buffer%\1658349_202003131343"
powershell -command "Start-Sleep -s 2"

xcopy /S /I /E "test\1674433_202003311328" "%buffer%\1674433_202003311328"
powershell -command "Start-Sleep -s 2"

xcopy /S /I /E "test\1686105_202004211301" "%buffer%\1686105_202004211301"
powershell -command "Start-Sleep -s 2"

xcopy /S /I /E "test\1687180_202004231102" "%buffer%\1687180_202004231102"
powershell -command "Start-Sleep -s 10"


REM Copy contents to fill directories

copy "test\1185840_202003250942 - %PassFail%" ProcessingBuffer\1185840_202003250942
powershell -command "Start-Sleep -s 2"

copy "test\1185841_202005070801 - %PassFail%" ProcessingBuffer\1185841_202005070801
powershell -command "Start-Sleep -s 2"

copy "test\1202741_202003101418 - %PassFail%" ProcessingBuffer\1202741_202003101418
powershell -command "Start-Sleep -s 2"

copy "test\1278061_202006301109 - %PassFail%" ProcessingBuffer\1278061_202006301109
powershell -command "Start-Sleep -s 2"

copy "test\1658348_202003111530 - %PassFail%" ProcessingBuffer\1658348_202003111530
powershell -command "Start-Sleep -s 2"

copy "test\1658349_202003131343 - %PassFail%" ProcessingBuffer\1658349_202003131343
powershell -command "Start-Sleep -s 2"

copy "test\1674433_202003311328 - %PassFail%" ProcessingBuffer\1674433_202003311328
powershell -command "Start-Sleep -s 2"

copy "test\1686105_202004211301 - %PassFail%" ProcessingBuffer\1686105_202004211301
powershell -command "Start-Sleep -s 2"

copy "test\1687180_202004231102 - %PassFail%" ProcessingBuffer\1687180_202004231102
