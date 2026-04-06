; TherapyTime Inno Setup script
; Version is read automatically from the published EXE — only update <Version> in TherapyTime.csproj.

#define MyAppName "TherapyTime"
#define MyAppVersion GetFileVersion("bin\Release\net10.0-windows\win-x64\publish\TherapyTime.exe")
#define MyAppPublisher "Nerd Brother LLC"
#define MyAppURL "https://github.com/TheBuilderHero/TherapyTime"
#define MyAppExeName "TherapyTime.exe"

[Setup]
AppId={{8A7D9194-74B1-4E2A-A3D7-120A86A5A0B1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=installer_output
OutputBaseFilename=TherapyTime_Setup_{#MyAppVersion}_x64
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
WizardStyle=modern
SetupIconFile=time-tracking.ico
UninstallDisplayIcon={app}\time-tracking.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "bin\Release\net10.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs
Source: "time-tracking.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\time-tracking.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\time-tracking.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
