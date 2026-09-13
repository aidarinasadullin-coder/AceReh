; Inno Setup Script — Калькулятор снеготаяния РЕХАУ
;
; Версия установщика берётся автоматически из FileVersion опубликованного exe
; (publish\SnowMeltingCalculator.exe). FileVersion задаётся <Version> в
; src/SnowMeltingCalculator.csproj — единственное место, где меняется версия.
; Папка publish\ должна существовать до компиляции установщика (см. INSTALL.md).

#define MyAppName "Калькулятор снеготаяния РЕХАУ"
#define MyAppPublisher "REHAU"
#define MyAppExeName "SnowMeltingCalculator.exe"

; FileVersion всегда четырёхчастная ("1.5.0.0"), а имя установщика исторически
; трёхчастное ("v1.5.0") — срезаем ровно один хвостовой ".0". Маркер "#"
; помечает конец строки, чтобы Pos нашёл именно хвостовое ".0".
#define VerRaw GetVersionNumbersString("..\publish\SnowMeltingCalculator.exe")
#define VerDot0Pos Pos(".0#", VerRaw + "#")
#if VerDot0Pos > 0
#define MyAppVersion Copy(VerRaw, 1, VerDot0Pos - 1)
#else
#define MyAppVersion VerRaw
#endif

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
OutputBaseFilename=SnowMeltingCalculator-v{#MyAppVersion}-Setup
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

[InstallDelete]
; Апгрейд поверх старой версии: перед копированием новых файлов полностью
; очищаем папку установки, чтобы не оставались файлы, которых больше нет в
; новой поставке (деинсталлятор удаляет только то, что записал сам).
; Поставка self-contained — всё содержимое {app} заново приходит из publish\*.
; Пользовательские данные в {app} не хранятся: настройки —
; %APPDATA%\SnowMeltingCalculator, шаблоны конструкций —
; %LOCALAPPDATA%\SnowMeltingCalculator, проекты .smc — у пользователя.
; ВАЖНО: всё, что вручную сохранено в папке установки, будет удалено.
Type: filesandordirs; Name: "{app}"

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

[Code]
// Защита от даунгрейда: если установлена более новая версия, предупреждаем
// и предлагаем прервать установку (по умолчанию — прервать).
function InitializeSetup(): Boolean;
var
  UninstKey, NewVersion, OldVersion, Msg: String;
begin
  Result := True;
  NewVersion := '{#SetupSetting("AppVersion")}';
  UninstKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' +
    '{#SetupSetting("AppId")}_is1';

  // Приложение ставится в 64-битный обзор (HKLM64); HKLM32 и HKCU оставлены
  // на случай per-user установки (PrivilegesRequiredOverridesAllowed=dialog).
  if RegQueryStringValue(HKLM64, UninstKey, 'DisplayVersion', OldVersion) or
     RegQueryStringValue(HKLM32, UninstKey, 'DisplayVersion', OldVersion) or
     RegQueryStringValue(HKCU64, UninstKey, 'DisplayVersion', OldVersion) or
     RegQueryStringValue(HKCU32, UninstKey, 'DisplayVersion', OldVersion) then
  begin
    if (OldVersion <> '') and (ComparePackedVersion(OldVersion, NewVersion) > 0) then
    begin
      Msg := 'На компьютере установлена более новая версия приложения «%s» — %s.' + #13#10 +
        'Устанавливаемая версия — %s.' + #13#10#13#10 +
        'Установка более старой версии поверх новой может привести к ' +
        'некорректной работе приложения. Продолжить установку?';
      if MsgBox(Format(Msg, ['{#MyAppName}', OldVersion, NewVersion]),
          mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDNO then
        Result := False;
    end;
  end;
end;
