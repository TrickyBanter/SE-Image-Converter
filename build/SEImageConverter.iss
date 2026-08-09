#ifndef AppVersion
#define AppVersion "1.1.0"
#endif

#ifndef PublishDir
#define PublishDir "..\artifacts\publish\SEImageConverter\win-x64"
#endif

#ifndef OutputDir
#define OutputDir "..\artifacts\release"
#endif

#ifndef SkipPrerequisites
#define SkipPrerequisites "false"
#endif

#ifndef DotNetDesktopRuntimeInstaller
#define DotNetDesktopRuntimeInstaller "prerequisites\windowsdesktop-runtime-win-x64.exe"
#endif

#ifndef WindowsAppRuntimeInstaller
#define WindowsAppRuntimeInstaller "prerequisites\WindowsAppRuntimeInstall-x64.exe"
#endif

#define AppName "SE Image Converter"
#define AppPublisher "SE Image Converter"
#define AppExeName "SE Image Converter.exe"
#define AppId "{{4D747F7D-B8A6-48C5-8B9C-34E14C694B7E}"
#define MinDotNetDesktopRuntimeVersion "10.0.0"
#define WindowsAppRuntimePackageName "MicrosoftCorporationII.WindowsAppRuntime.Main.2.3"

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
#if SkipPrerequisites != "true"
Source: "{#DotNetDesktopRuntimeInstaller}"; DestName: "windowsdesktop-runtime-win-x64.exe"; Flags: dontcopy
Source: "{#WindowsAppRuntimeInstaller}"; DestName: "WindowsAppRuntimeInstall-x64.exe"; Flags: dontcopy
#endif

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

#if SkipPrerequisites != "true"
[Code]
function ReadVersionPart(var Version: String): Integer;
var
  PartEnd: Integer;
  Part: String;
begin
  PartEnd := Pos('.', Version);

  if PartEnd > 0 then
  begin
    Part := Copy(Version, 1, PartEnd - 1);
    Delete(Version, 1, PartEnd);
  end
    else
  begin
    Part := Version;
    Version := '';
  end;

  if Part = '' then
  begin
    Result := 0;
  end
    else
  begin
    Result := StrToInt(Part);
  end;
end;

function CompareVersionStrings(InstalledVersion, RequiredVersion: String): Integer;
var
  InstalledPart: Integer;
  RequiredPart: Integer;
begin
  Result := 0;

  while (Result = 0) and ((InstalledVersion <> '') or (RequiredVersion <> '')) do
  begin
    InstalledPart := ReadVersionPart(InstalledVersion);
    RequiredPart := ReadVersionPart(RequiredVersion);

    if InstalledPart < RequiredPart then
    begin
      Result := -1;
    end
      else if InstalledPart > RequiredPart then
    begin
      Result := 1;
    end;
  end;
end;

function IsDotNetDesktopRuntimeInstalled(): Boolean;
var
  Versions: TArrayOfString;
  I: Integer;
begin
  Result := False;

  if not RegGetSubkeyNames(
    HKLM64,
    'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App',
    Versions) then
  begin
    Exit;
  end;

  for I := 0 to GetArrayLength(Versions) - 1 do
  begin
    if CompareVersionStrings(Versions[I], '{#MinDotNetDesktopRuntimeVersion}') >= 0 then
    begin
      Result := True;
      Exit;
    end;
  end;
end;

function IsWindowsAppRuntimeInstalled(): Boolean;
var
  ResultCode: Integer;
begin
  Exec(
    ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    '-NoProfile -ExecutionPolicy Bypass -Command "if (Get-AppxPackage -Name ''{#WindowsAppRuntimePackageName}'') { exit 0 } else { exit 1 }"',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode);

  Result := ResultCode = 0;
end;

function InstallPrerequisite(FileName, Parameters, DisplayName: String): String;
var
  ResultCode: Integer;
begin
  Result := '';
  ExtractTemporaryFile(FileName);

  WizardForm.StatusLabel.Caption := 'Installing ' + DisplayName + '...';
  WizardForm.ProgressGauge.Style := npbstMarquee;

  if not Exec(
    ExpandConstant('{tmp}\' + FileName),
    Parameters,
    '',
    SW_SHOW,
    ewWaitUntilTerminated,
    ResultCode) then
  begin
    Result := 'Setup could not start the ' + DisplayName + ' installer.';
    Exit;
  end;

  if (ResultCode <> 0) and (ResultCode <> 3010) then
  begin
    Result := DisplayName + ' installation failed with exit code ' + IntToStr(ResultCode) + '.';
    Exit;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Prerequisite: String;
begin
  Result := '';

  if not IsDotNetDesktopRuntimeInstalled() then
  begin
    Prerequisite := InstallPrerequisite(
      ExtractFileName('{#DotNetDesktopRuntimeInstaller}'),
      '/install /quiet /norestart',
      '.NET Desktop Runtime {#MinDotNetDesktopRuntimeVersion} or newer');

    if Prerequisite <> '' then
    begin
      Result := Prerequisite;
      Exit;
    end;
  end;

  if not IsWindowsAppRuntimeInstalled() then
  begin
    Prerequisite := InstallPrerequisite(
      ExtractFileName('{#WindowsAppRuntimeInstaller}'),
      '--quiet',
      'Windows App Runtime');

    if Prerequisite <> '' then
    begin
      Result := Prerequisite;
      Exit;
    end;
  end;
end;
#endif
