#define AppName "iPhone Drive"
#define AppVersion "1.0.0"

[Setup]
AppId={{C19509F0-2045-4A22-9EA7-D16419B13D53}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=essa_tareen
DefaultDirName={autopf}\iPhoneDrive
DefaultGroupName={#AppName}
OutputDir=..\release
OutputBaseFilename=iPhoneDrive-Setup-{#AppVersion}
SetupIconFile=..\assets\iphone-drive.ico
UninstallDisplayIcon={app}\iPhoneDrive.exe
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
LicenseFile=..\LICENSE

[Tasks]
Name: startup; Description: "Start iPhone Drive when Windows starts"; Flags: checkedonce

[Files]
Source: "..\payload\*"; DestDir: "{app}"; Excludes: "Dokan_x64.msi"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\payload\Dokan_x64.msi"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{group}\iPhone Drive"; Filename: "{app}\iPhoneDrive.exe"

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "iPhoneDrive"; ValueData: """{app}\iPhoneDrive.exe"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{sys}\msiexec.exe"; Parameters: "/i ""{tmp}\Dokan_x64.msi"" /qn /norestart"; StatusMsg: "Installing Dokan..."; Flags: runhidden waituntilterminated; Check: not DokanInstalled
Filename: "{app}\iPhoneDrive.exe"; Description: "Start iPhone Drive"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM iPhoneDrive.exe"; Flags: runhidden; RunOnceId: StopTray
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM ifuse-win.exe"; Flags: runhidden; RunOnceId: StopAfc
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM aggregate-proxy.exe"; Flags: runhidden; RunOnceId: StopProxy

[Code]
function AppleDriverInstalled: Boolean;
begin
  Result := RegKeyExists(HKLM64, 'SYSTEM\CurrentControlSet\Services\Apple Mobile Device Service') or
            FileExists(ExpandConstant('{commonpf64}\Apple\Mobile Device Support\Drivers\usbaapl64.sys'));
end;

function DokanInstalled: Boolean;
begin
  Result := FileExists(ExpandConstant('{sys}\drivers\dokan2.sys'));
end;

function InitializeSetup: Boolean;
var ErrorCode: Integer;
begin
  Result := AppleDriverInstalled;
  if not Result then begin
    MsgBox('Apple Devices and its mobile-device driver must be installed first. The official Apple instructions will now open. Install Apple Devices from Microsoft Store, then run this installer again.', mbInformation, MB_OK);
    ShellExec('open', 'https://support.apple.com/guide/devices-windows/install-the-apple-devices-app-mchl5ded2763/windows', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;
end;
