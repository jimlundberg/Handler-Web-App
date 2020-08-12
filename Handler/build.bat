cd C:\SSMCharacterizationHandler\Handler
DOTNET build handler
XCOPY /S /Q /H /R /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\bin\Release\netcoreapp3.1 C:\SSMCharacterizationHandler\Handler
XCOPY /S /Q /H /R /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\wwwroot C:\SSMCharacterizationHandler\Handler
XCOPY /S /Q /H /R /Y C:\SSMCharacterizationHandler\Handler\Handler\Handler\properties C:\SSMCharacterizationHandler\Handler
rmdir /S /Q de
rmdir /S /Q es
rmdir /S /Q fr
rmdir /S /Q it
rmdir /S /Q ja
rmdir /S /Q ko
rmdir /S /Q pl
rmdir /S /Q pt-BR
rmdir /S /Q ru
rmdir /S /Q tr
cd ..
