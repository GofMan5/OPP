#define MyAppName "OPP"
#define MyAppVersion "1.0.5"
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
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=Output
OutputBaseFilename=OPP_Setup_{#MyAppVersion}
SetupIconFile=OPP.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
; Параметры обновления
CloseApplications=force
RestartApplications=no
UsePreviousAppDir=yes
UpdateUninstallLogAppName=yes

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Основные файлы приложения
Source: "App\bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Предлагаем запустить
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall

[InstallDelete]
; Удаляем старые файлы перед обновлением
Type: files; Name: "{app}\*.dll"
Type: filesandordirs; Name: "{app}\runtimes"

[Code]
const
  WM_CLOSE = 16;

function InitializeSetup(): Boolean;
begin
  Result := True;
end; 