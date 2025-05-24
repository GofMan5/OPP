@echo off
echo Сборка OPP...

echo Получение версии из appsettings.json...
for /f %%i in ('powershell -ExecutionPolicy Bypass -File GetVersion.ps1') do set VERSION=%%i
echo Текущая версия: %VERSION%

echo Обновление версии в OPP_Setup.iss...
powershell -Command "(Get-Content OPP_Setup.iss) -replace '#define MyAppVersion \".*\"', '#define MyAppVersion \"%VERSION%\"' | Set-Content OPP_Setup.iss"

echo Сборка проекта...
dotnet build App/App.csproj -c Release

echo Сборка установщика...
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "OPP_Setup.iss"

echo Готово! Установщик создан: Output\OPP_Setup_%VERSION%.exe 