cd C:\SSMCharacterizationHandler\Handler
dotnet publish -c "Release" -r "win-x64" handler
XCOPY /S /Q /H /R /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\bin\Release\netcoreapp3.1\win-x64\publish C:\SSMCharacterizationHandler\Handler
copy C:\SSMCharacterizationHandler\Handler\Handler\Handler\Config.ini C:\SSMCharacterizationHandler\Handler
cd ..
