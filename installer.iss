[Setup]
AppName=SystemCtrl
AppVersion=1.0
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

[Files]
Source: "SystemCtrl.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\SystemCtrl"; Filename: "{app}\SystemCtrl.exe"
Name: "{autodesktop}\SystemCtrl"; Filename: "{app}\SystemCtrl.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Run]
Filename: "{app}\SystemCtrl.exe"; Description: "Launch SystemCtrl"; Flags: nowait postinstall skipifsilent
