#define MyAppName "Handheld Launcher"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "Handheld Launcher contributors"
#define MyAppExeName "HandheldLauncher.exe"

[Setup]
AppId={{E9179595-8B6B-4F34-9A48-CF374059125E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\Handheld Launcher
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\outputs
OutputBaseFilename=HandheldLauncher-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "..\dist\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Parameters: "--setup"; Description: "Choose game folders and optionally add to Steam"; Flags: nowait postinstall skipifsilent
