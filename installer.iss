#ifndef MyAppVersion
#define MyAppVersion "1.0.0-dev"
#endif

[Setup]
AppId={{B5E5D7A2-8F3C-4A1E-9D6B-2C7F8E4A1B3D}
AppName=SystemCtrl
AppVersion={#MyAppVersion}
AppPublisher=SystemCtrl
DefaultDirName={autopf}\SystemCtrl
DefaultGroupName=SystemCtrl
OutputDir=Output
OutputBaseFilename=SystemCtrl-Setup
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
SetupIconFile=src\SystemCtrl.Desktop\Assets\logo.ico
UninstallDisplayIcon={app}\SystemCtrl.exe
DisableProgramGroupPage=yes
UsePreviousAppDir=yes
CloseApplications=yes
RestartApplications=yes

[Files]
Source: "installer_publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\SystemCtrl"; Filename: "{app}\SystemCtrl.exe"
Name: "{autodesktop}\SystemCtrl"; Filename: "{app}\SystemCtrl.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Run]
Filename: "{app}\SystemCtrl.exe"; Description: "Launch SystemCtrl"; Flags: nowait postinstall skipifsilent
