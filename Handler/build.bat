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

REM Copy the Publish directory to the base handler directory
XCOPY /S /Q /H /R /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\bin\Release\netcoreapp3.1\win-x64\publish C:\SSMCharacterizationHandler\Handler

REM Copy the Publish directory to the base handler directory
copy C:\SSMCharacterizationHandler\Handler\Handler\Handler\Config.ini C:\SSMCharacterizationHandler\Handler

REM Remove the build files after copying them
rmdir /Q /S C:\SSMCharacterizationHandler\Handler\Handler\Handler\bin

REM Copy the test batch files to the project directory
copy /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\build.bat C:\SSMCharacterizationHandler
copy /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\clear.bat C:\SSMCharacterizationHandler
copy /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\tcp.bat C:\SSMCharacterizationHandler
copy /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\test.bat C:\SSMCharacterizationHandler
copy /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\test1.bat C:\SSMCharacterizationHandler
copy /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\test2.bat C:\SSMCharacterizationHandler

cd ..

systeminfo
