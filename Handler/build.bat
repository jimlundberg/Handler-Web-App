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

dotnet publish -c "Release" -r "win-x64" handler
XCOPY /S /Q /H /R /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\bin\Release\netcoreapp3.1\win-x64\publish C:\SSMCharacterizationHandler\Handler
copy C:\SSMCharacterizationHandler\Handler\Handler\Handler\Config.ini C:\SSMCharacterizationHandler\Handler
rmdir /Q /S C:\SSMCharacterizationHandler\Handler\Handler\Handler\bin
cd ..

systeminfo
