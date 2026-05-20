[Setup]
AppName=OverlayEngine
AppVersion=1.1.0
AppPublisher=drdrajwer
AppId={{B3F2A1C4-7E8D-4F6A-9B2E-1C3D5E7F8A0B}
DefaultDirName={autopf}\OverlayEngine
DefaultGroupName=OverlayEngine
OutputDir=releases
OutputBaseFilename=OverlayEngine-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
CloseApplications=yes
UninstallDisplayIcon={app}\OverlayEngine.UI.exe

[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"

[Files]
Source: "publish_installer\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\OverlayEngine";         Filename: "{app}\OverlayEngine.UI.exe"
Name: "{commondesktop}\OverlayEngine"; Filename: "{app}\OverlayEngine.UI.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Utwórz skrót na pulpicie"; GroupDescription: "Dodatkowe ikony:"

[Run]
Filename: "{app}\OverlayEngine.UI.exe"; \
  Description: "Uruchom OverlayEngine"; \
  Flags: nowait postinstall skipifsilent runascurrentuser

[UninstallRun]
Filename: "taskkill.exe"; Parameters: "/f /im OverlayEngine.UI.exe"; Flags: runhidden
