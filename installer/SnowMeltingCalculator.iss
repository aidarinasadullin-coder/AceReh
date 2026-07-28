; Inno Setup Script — Калькулятор снеготаяния РЕХАУ v1.1.1

#define MyAppName "Калькулятор снеготаяния РЕХАУ"
#define MyAppVersion "1.1.1"
#define MyAppPublisher "REHAU"
#define MyAppExeName "SnowMeltingCalculator.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\REHAU\SnowMeltingCalculator
DefaultGroupName=REHAU\Калькулятор снеготаяния
DisableProgramGroupPage=yes
LicenseFile=..\docs\license.rtf
OutputDir=..\output
OutputBaseFilename=SnowMeltingCalculator-v1.1.1-Setup
SetupIconFile=..\src\Assets\app_icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Полный self-contained publish, включая runtime и все вложенные ресурсы
Source: "..\publish\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Ассоциация расширения .smc с приложением
Root: HKA; Subkey: "Software\Classes\.smc"; ValueType: string; ValueName: ""; ValueData: "SnowMeltingCalculator.Project"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\.smc\OpenWithProgids"; ValueType: string; ValueName: "SnowMeltingCalculator.Project"; ValueData: ""; Flags: uninsdeletevalue

; ProgID для файлов проекта
Root: HKA; Subkey: "Software\Classes\SnowMeltingCalculator.Project"; ValueType: string; ValueName: ""; ValueData: "Проект Калькулятора снеготаяния РЕХАУ"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SnowMeltingCalculator.Project\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SnowMeltingCalculator.Project\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Flags: uninsdeletekey

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Удалять каталоги только если в них не осталось пользовательских файлов
Type: dirifempty; Name: "{app}"
Type: dirifempty; Name: "{autopf}\REHAU"
