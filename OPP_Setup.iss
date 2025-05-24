#define MyAppName "OPP"
#define MyAppVersion "1.0.3"
#define MyAppPublisher "GofMan3"
#define MyAppURL "https://www.youtube.com/@GofMan3"
#define MyAppExeName "App.exe"

[Setup]
; Уникальный идентификатор приложения (используется для удаления)
AppId={{98761AB5-CD45-4F3A-B987-D271BB8E1BAA}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
; Запрос прав администратора
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=Output
OutputBaseFilename=OPP_Setup_{#MyAppVersion}
SetupIconFile=OPP.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
; Добавление информации о версии в exe файл установщика
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoCopyright={#MyAppPublisher}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
; Параметры обновления
CloseApplications=force
RestartApplications=no
; Сообщение о закрытии приложения (если запущено)
CloseApplicationsFilter=*.exe
; Задержка перед закрытием приложения
RestartIfNeededByRun=yes
; Удаление предыдущих версий перед установкой
UsePreviousAppDir=yes
; Разрешаем изменение и удаление
UsePreviousSetupType=yes
; Сохранение настроек между обновлениями
UpdateUninstallLogAppName=yes
AllowCancelDuringInstall=yes

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; Основные файлы приложения
Source: "App\bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Обновляем appsettings.json чтобы версия обновилась
Source: "App\bin\Release\net8.0-windows\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Если это новая установка, предлагаем запустить
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall; Check: not IsUpdate

; Если это обновление, автоматически запускаем без запроса
Filename: "{app}\{#MyAppExeName}"; Flags: nowait postinstall runasoriginaluser; Check: IsUpdate

[InstallDelete]
; Удаляем старые файлы перед обновлением
Type: files; Name: "{app}\*.dll"
Type: filesandordirs; Name: "{app}\runtimes"

[Code]
// Проверка, установлена ли программа (для определения режима обновления)
function IsUpdate(): Boolean;
var
  PrevPath: String;
begin
  Result := False;
  if RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1', 'Inno Setup: App Path', PrevPath) then
  begin
    Result := True;
    Log('Обнаружена предыдущая установка: ' + PrevPath);
  end
  else 
  begin
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1', 'Inno Setup: App Path', PrevPath) then
    begin
      Result := True;
      Log('Обнаружена предыдущая установка: ' + PrevPath);
    end;
  end;
end;

// Функция для завершения процесса приложения перед установкой (при необходимости)
procedure KillAppProcess();
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/F /IM App.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(1000); // Даем время на закрытие процесса
end;

// Событие перед установкой
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    // Завершаем процесс приложения перед установкой (если запущен)
    KillAppProcess();
  end;
end;

// Проверка наличия .NET Framework
function IsDotNetDetected(version: string; service: cardinal): boolean;
var
    key, versionKey: string;
    install, release, serviceCount: cardinal;
    success: boolean;
begin
    versionKey := version;
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + versionKey;
    success := RegQueryDWordValue(HKLM, key, 'Install', install);
    success := success and RegQueryDWordValue(HKLM, key, 'Release', release);
    success := success and (install = 1) and (release >= 528040);
    Result := success;
end;

// Проверка при старте установки
function InitializeSetup(): Boolean;
begin
    // Проверяем наличие .NET Framework и закрываем приложение если оно запущено
    if not IsDotNetDetected('v4\Full', 0) then
    begin
        MsgBox('Для работы приложения требуется Microsoft .NET Framework 4.8.'#13#13
            'Пожалуйста, установите его и повторите установку.', mbInformation, MB_OK);
        Result := False;
    end
    else
    begin
        KillAppProcess();
        Result := True;
    end;
end; 
