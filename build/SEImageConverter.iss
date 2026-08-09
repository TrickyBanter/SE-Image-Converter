#ifndef AppVersion
#define AppVersion "1.0.0"
#endif

#ifndef PublishDir
#define PublishDir "..\artifacts\publish\SEImageConverter\win-x64"
#endif

#ifndef OutputDir
#define OutputDir "..\artifacts\release"
#endif

#define AppName "SE Image Converter"
#define AppPublisher "SE Image Converter"
#define AppExeName "ImageConversion.App.exe"
#define AppId "{{4D747F7D-B8A6-48C5-8B9C-34E14C694B7E}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\SE Image Converter
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=SEImageConverter-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\src\ImageConversion.App\Assets\AppIcon.ico
UninstallDisplayIcon={app}\Assets\AppIcon.ico
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
