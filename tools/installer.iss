; Inno Setup script that packages pixory into a small per-user installer.
;
; Build it with:
;   "ISCC.exe" /DAppVersion=1.0.0 installer.iss
; The self-contained pixory.exe must already be published to dist\win-x64
; (run tools\publish.ps1 first). The release.ps1 script does both in order.
;
; Per-user install (PrivilegesRequired=lowest) is deliberate: it never raises a
; UAC prompt, so the in-app auto-update can run the installer without the user
; having to approve an elevation every time.

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#define AppName "pixory"
#define AppPublisher "Volkan Turhan"
#define AppUrl "https://github.com/volkanturhan/pixory"
#define AppExe "pixory.exe"

[Setup]
; A fixed AppId ties upgrades and the uninstall entry together across versions.
; Never change it once shipped.
AppId={{818A9898-CFE1-493F-823D-7F38E0902D51}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}
WizardStyle=modern
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Let Inno's Restart Manager close a running pixory before overwriting its exe.
CloseApplications=yes
SetupIconFile=..\pixory\Assets\pixory.ico
UninstallDisplayIcon={app}\{#AppExe}
OutputDir=..\dist\installer
OutputBaseFilename=pixory-setup-v{#AppVersion}
Compression=lzma2/max
SolidCompression=yes

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "tr"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\dist\win-x64\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
; Offer to launch pixory when the wizard finishes (after a manual install).
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
