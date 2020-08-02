cd C:\SSMCharacterizationHandler\Handler
DOTNET build handler
XCOPY /S /Q /H /R /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\bin\Release\netcoreapp3.1 C:\SSMCharacterizationHandler\Handler
XCOPY /S /Q /H /R /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\wwwroot C:\SSMCharacterizationHandler\Handler
XCOPY /S /Q /H /R /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\properties C:\SSMCharacterizationHandler\Handler
