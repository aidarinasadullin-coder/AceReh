; Inno Setup Script — Калькулятор снеготаяния РЕХАУ v1.0.0

#define MyAppName "Калькулятор снеготаяния РЕХАУ"
#define MyAppVersion "1.0.0"
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
OutputBaseFilename=SnowMeltingCalculator-v1.0-Setup
SetupIconFile=..\resources\РЕХАУ_logo.ico
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
; Основной EXE (self-contained, ~200 MB)
Source: "..\publish\SnowMeltingCalculator.exe"; DestDir: "{app}"; Flags: ignoreversion
; PDB (отладочные символы)
Source: "..\publish\SnowMeltingCalculator.pdb"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; Иконка приложения и файлов проекта
Source: "..\resources\РЕХАУ_logo.ico"; DestDir: "{app}"; Flags: ignoreversion
; Данные
Source: "..\publish\data\climate_db.json"; DestDir: "{app}\data"; Flags: ignoreversion
Source: "..\publish\data\glycol_data.json"; DestDir: "{app}\data"; Flags: ignoreversion
Source: "..\publish\data\materials_db.json"; DestDir: "{app}\data"; Flags: ignoreversion
Source: "..\publish\data\rehau_products.json"; DestDir: "{app}\data"; Flags: ignoreversion
; Шрифты Lato
Source: "..\publish\LatoFont\*"; DestDir: "{app}\LatoFont"; Flags: ignoreversion recursesubdirs

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
Root: HKA; Subkey: "Software\Classes\SnowMeltingCalculator.Project\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\РЕХАУ_logo.ico"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SnowMeltingCalculator.Project\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Flags: uninsdeletekey

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Удаление пустых папок при деинсталляции
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // Удалить папку data если пустая
    DataDir := ExpandConstant('{app}\data');
    if DirExists(DataDir) and (Length(RemoveDir(DataDir)) > 0) then
      RemoveDir(DataDir);
    
    // Удалить папку LatoFont если пустая
    DataDir := ExpandConstant('{app}\LatoFont');
    if DirExists(DataDir) then
      RemoveDir(DataDir);
    
    // Удалить папку приложения если пустая
    DataDir := ExpandConstant('{app}');
    if DirExists(DataDir) then
      RemoveDir(DataDir);
    
    // Удалить папку REHAU если пустая
    DataDir := ExpandConstant('{autopf}\REHAU');
    if DirExists(DataDir) then
      RemoveDir(DataDir);
  end;
end;
