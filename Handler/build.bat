@echo off

cd C:\SSMCharacterizationHandler\Handler

REM Delete all but the Handler directory
pushd "C:\SSMCharacterizationHandler\Handler" || exit /B 1
for /D %%D in ("*") do (
    if /I not "%%~nxD"=="Handler" rd /S /Q "%%~D"
)
for %%F in ("*") do (
    del /Q "%%~F"
)
popd

REM Run the DOTNET publisher command in x64 release mode
dotnet publish -c "Release" -r "win-x64" handler

@echo on

REM Copy the Handler ASP.NET Publish directory to the handler directory...
XCOPY /S /Q /H /R /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\bin\Release\netcoreapp3.1\win-x64\publish C:\SSMCharacterizationHandler\Handler

REM Copy the Handler configuration config.ini file to the handler directory...
copy C:\SSMCharacterizationHandler\Handler\Handler\Handler\Config.ini C:\SSMCharacterizationHandler\Handler

REM Copy the Handler test batch files to the project directory...
copy /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\Build.bat C:\SSMCharacterizationHandler
copy /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\Clear.bat C:\SSMCharacterizationHandler
copy /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\Tcp.bat C:\SSMCharacterizationHandler
copy /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\InputPartialTest.bat C:\SSMCharacterizationHandler
copy /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\InputFullTest.bat C:\SSMCharacterizationHandler
copy /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\InputBothTest.bat C:\SSMCharacterizationHandler

REM Delete the Handler bin directory build files after we are done with them...
rmdir /Q /S C:\SSMCharacterizationHandler\Handler\Handler\Handler\bin

cd ..

systeminfo
