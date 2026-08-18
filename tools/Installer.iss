; =====================================================================
; IronXNestCommand - Inno Setup Script
; Co-op Lobby Overlay, Enemy Despawn Guard & Punchcard Sync
; =====================================================================

#define MyAppName "IronXNestCommand"
#define MyAppVersion "0.1.1"
#define MyAppPublisher "IronX Team"
#define MyAppURL "https://github.com/Brennerofhell/IronXNestCommand"
#define GameExe "Iron Nest Heavy Turret Simulator.exe"

[Setup]
AppId={{C514B16C-10F1-49D9-B3BE-892E634E74FC}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={code:GetDefaultGameDir}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=IronXNestCommand_Setup_v{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayName={#MyAppName} - Co-op Mod & Overlay
UninstallDisplayIcon={app}\{#GameExe}

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "full"; Description: "Vollstaendige Installation (BepInEx 6 IL2CPP)"
Name: "custom"; Description: "Benutzerdefiniert"; Flags: iscustom

[Components]
Name: "bepinex"; Description: "IronXNestCommand BepInEx 6 Host (Standard / Empfohlen)"; Types: full custom; Flags: fixed
Name: "melon"; Description: "IronXNestCommand MelonLoader 0.7.3 Host (Optional)"; Types: custom

[Files]
; BepInEx Payload
Source: "..\IronXNestCommand.Host.BepInEx\bin\Release\IronXNestCommand.dll"; DestDir: "{app}\BepInEx\plugins"; Components: bepinex; Flags: ignoreversion
Source: "..\IronXNestCommand.Core\bin\Release\IronXNestCommand.Core.dll"; DestDir: "{app}\BepInEx\plugins"; Components: bepinex; Flags: ignoreversion

; MelonLoader Payload
Source: "..\IronXNestCommand.MelonLoader\bin\Release\IronXNestCommand.dll"; DestDir: "{app}\Mods"; Components: melon; Flags: ignoreversion

; Mod Manager GUI & Batch Tools
Source: "..\tools\ModManagerGUI.ps1"; DestDir: "{app}\UserData\IronXNestCommand\tools"; Flags: ignoreversion
Source: "..\Uninstall-GUI.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}\UserData\IronXNestCommand"; Flags: ignoreversion

[Icons]
Name: "{group}\IronX Mod Manager GUI"; Filename: "{app}\Uninstall-GUI.bat"
Name: "{group}\Deinstallieren"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#GameExe}"; Description: "Iron Nest: Heavy Turret Simulator jetzt starten"; Flags: nowait postinstall skipifsilent unchecked

[UninstallDelete]
Type: files; Name: "{app}\BepInEx\plugins\IronXNestCommand.dll"
Type: files; Name: "{app}\BepInEx\plugins\IronXNestCommand.Core.dll"
Type: files; Name: "{app}\Mods\IronXNestCommand.dll"

[Code]
// Auto-Erkennung des Steam-Installationsordners fuer Iron Nest
function GetDefaultGameDir(Param: String): String;
var
  SteamPath: String;
  CheckPath: String;
  Drives: array of String;
  I: Integer;
begin
  // 1. Pruefe Standardpfad auf C:
  CheckPath := 'C:\Program Files (x86)\Steam\steamapps\common\Iron Nest Heavy Turret Simulator';
  if FileExists(CheckPath + '\{#GameExe}') then
  begin
    Result := CheckPath;
    Exit;
  end;

  CheckPath := 'C:\Program Files\Steam\steamapps\common\Iron Nest Heavy Turret Simulator';
  if FileExists(CheckPath + '\{#GameExe}') then
  begin
    Result := CheckPath;
    Exit;
  end;

  // 2. Pruefe Registry-Eintraege fuer Steam
  if RegQueryStringValue(HKEY_CURRENT_USER, 'Software\Valve\Steam', 'SteamPath', SteamPath) or
     RegQueryStringValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\Valve\Steam', 'InstallPath', SteamPath) or
     RegQueryStringValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\WOW6432Node\Valve\Steam', 'InstallPath', SteamPath) then
  begin
    CheckPath := SteamPath + '\steamapps\common\Iron Nest Heavy Turret Simulator';
    if FileExists(CheckPath + '\{#GameExe}') then
    begin
      Result := CheckPath;
      Exit;
    end;
  end;

  // 3. Durchsuche gaengige SteamLibrary-Pfade auf anderen Laufwerken
  SetArrayLength(Drives, 5);
  Drives[0] := 'D:';
  Drives[1] := 'E:';
  Drives[2] := 'F:';
  Drives[3] := 'G:';
  Drives[4] := 'H:';

  for I := 0 to GetArrayLength(Drives) - 1 do
  begin
    CheckPath := Drives[I] + '\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator';
    if FileExists(CheckPath + '\{#GameExe}') then
    begin
      Result := CheckPath;
      Exit;
    end;
  end;

  // Fallback, wenn das Spiel nicht automatisch gefunden wurde
  Result := 'C:\Program Files (x86)\Steam\steamapps\common\Iron Nest Heavy Turret Simulator';
end;

// Validierung: Warnung falls das ausgewaehlte Verzeichnis die Game-Exe nicht enthaelt
function NextButtonClick(CurPageID: Integer): Boolean;
var
  SelectedDir: String;
begin
  Result := True;
  if CurPageID = wpSelectDir then
  begin
    SelectedDir := WizardDirValue();
    if not FileExists(SelectedDir + '\{#GameExe}') then
    begin
      if MsgBox('Im gewaehlten Ordner wurde "' + '{#GameExe}' + '" nicht gefunden.' + #13#10 + #13#10 +
                'Moechtest du die Mod trotzdem in diesen Ordner installieren?', mbConfirmation, MB_YESNO) = IDNO then
      begin
        Result := False;
      end;
    end;
  end;
end;
