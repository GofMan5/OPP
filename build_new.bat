@echo off
echo Сборка OPP...

echo Получение версии из appsettings.json...
for /f "tokens=2 delims=:," %%a in ('type "App\appsettings.json" ^| findstr "Version"') do (
    set VERSION=%%a
    set VERSION=!VERSION:"=!
    set VERSION=!VERSION: =!
)
echo Текущая версия: %VERSION%

echo Обновление версии в OPP_Setup_New.iss...
powershell -Command "(Get-Content OPP_Setup_New.iss) -replace '#define MyAppVersion \".*\"', '#define MyAppVersion \"%VERSION%\"' | Set-Content OPP_Setup_New.iss"

echo Сборка проекта...
dotnet build App -c Release

echo Сборка установщика...
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" OPP_Setup_New.iss

echo Готово! Установщик создан: Output\OPP_Setup_%VERSION%.exe 