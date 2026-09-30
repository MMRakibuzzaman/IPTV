[Setup]
AppName=IPTV Player
AppVersion=1.0
DefaultDirName={autopf}\IPTV Player
DefaultGroupName=IPTV Player
OutputBaseFilename=IPTV_Setup_Windows
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
DisableProgramGroupPage=yes

[Files]
Source: "publish-desktop\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\IPTV Player"; Filename: "{app}\IPTV.Desktop.exe"
Name: "{autodesktop}\IPTV Player"; Filename: "{app}\IPTV.Desktop.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\IPTV.Desktop.exe"; Description: "Launch IPTV Player"; Flags: nowait postinstall skipifsilent
