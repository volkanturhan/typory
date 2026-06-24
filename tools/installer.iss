; Inno Setup script that packages typory into a small per-user installer.
;
; Build it with:
;   "ISCC.exe" /DAppVersion=1.0.0 installer.iss
; The self-contained typory.exe must already be published to dist\win-x64
; (run tools\publish.ps1 first). The release.ps1 script does both in order.
;
; Per-user install (PrivilegesRequired=lowest) is deliberate: it never raises a
; UAC prompt, so the in-app auto-update can run the installer without the user
; having to approve an elevation every time.

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#define AppName "typory"
#define AppPublisher "Volkan Turhan"
#define AppUrl "https://github.com/volkanturhan/typory"
#define AppExe "typory.exe"

[Setup]
; A fixed AppId ties upgrades and the uninstall entry together across versions.
; Never change it once shipped.
AppId={{69DBE74E-1B64-4EB8-B65C-0198DE9AC46C}
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
; Let Inno's Restart Manager close a running typory before overwriting its exe.
CloseApplications=yes
SetupIconFile=..\typory\Assets\typory.ico
UninstallDisplayIcon={app}\{#AppExe}
OutputDir=..\dist\installer
OutputBaseFilename=typory-setup-v{#AppVersion}
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
; Offer to launch typory when the wizard finishes (after a manual install).
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
