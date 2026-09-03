#ifndef AppVersion
#define AppVersion "2.0.1"
#endif

#ifndef PublishDir
#define PublishDir "..\artifacts\publish\SEToolkit\win-x64"
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

#define AppName "SE Toolkit"
#define AppPublisher "SE Toolkit"
#define AppExeName "SE Toolkit.exe"
#define AppId "{{4D747F7D-B8A6-48C5-8B9C-34E14C694B7E}"
#define LegacyAppName "SE Image Converter"
#define MinDotNetDesktopRuntimeVersion "10.0.0"
#define WindowsAppRuntimePackageName "MicrosoftCorporationII.WindowsAppRuntime.Main.2.3"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\SE Toolkit
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=SEToolkit-Setup-{#AppVersion}
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

[InstallDelete]
Type: filesandordirs; Name: "{autoprograms}\{#LegacyAppName}"
Type: files; Name: "{autodesktop}\{#LegacyAppName}.lnk"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
const
  UninstallRegistryPath = 'Software\Microsoft\Windows\CurrentVersion\Uninstall';

function SplitCommandLine(CommandLine: String; var FileName: String; var Parameters: String): Boolean;
var
  QuoteEnd: Integer;
  SpacePosition: Integer;
begin
  CommandLine := Trim(CommandLine);
  FileName := '';
  Parameters := '';

  if CommandLine = '' then
  begin
    Result := False;
    Exit;
  end;

  if Copy(CommandLine, 1, 1) = '"' then
  begin
    QuoteEnd := Pos('"', Copy(CommandLine, 2, Length(CommandLine) - 1));

    if QuoteEnd = 0 then
    begin
      Result := False;
      Exit;
    end;

    FileName := Copy(CommandLine, 2, QuoteEnd - 1);
    Parameters := Trim(Copy(CommandLine, QuoteEnd + 2, Length(CommandLine)));
  end
    else
  begin
    SpacePosition := Pos(' ', CommandLine);

    if SpacePosition = 0 then
    begin
      FileName := CommandLine;
    end
      else
    begin
      FileName := Copy(CommandLine, 1, SpacePosition - 1);
      Parameters := Trim(Copy(CommandLine, SpacePosition + 1, Length(CommandLine)));
    end;
  end;

  Result := FileName <> '';
end;

function ExecuteCommandLine(CommandLine: String; var ResultCode: Integer): Boolean;
var
  FileName: String;
  Parameters: String;
begin
  if not SplitCommandLine(CommandLine, FileName, Parameters) then
  begin
    Result := False;
    Exit;
  end;

  Result := Exec(
    FileName,
    Parameters,
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode);
end;

function ReadUninstallCommand(RootKey: Integer; Subkey: String; var CommandLine: String): Boolean;
var
  EntryPath: String;
begin
  EntryPath := UninstallRegistryPath + '\' + Subkey;

  if RegQueryStringValue(RootKey, EntryPath, 'QuietUninstallString', CommandLine) and
    (CommandLine <> '') then
  begin
    Result := True;
    Exit;
  end;

  Result := RegQueryStringValue(RootKey, EntryPath, 'UninstallString', CommandLine) and
    (CommandLine <> '');

  if Result then
  begin
    CommandLine := CommandLine + ' /VERYSILENT /SUPPRESSMSGBOXES /NORESTART';
  end;
end;

function UninstallLegacyFromRoot(RootKey: Integer): String;
var
  CommandLine: String;
  DisplayName: String;
  I: Integer;
  ResultCode: Integer;
  Subkeys: TArrayOfString;
begin
  Result := '';

  if not RegGetSubkeyNames(RootKey, UninstallRegistryPath, Subkeys) then
  begin
    Exit;
  end;

  for I := 0 to GetArrayLength(Subkeys) - 1 do
  begin
    if RegQueryStringValue(RootKey, UninstallRegistryPath + '\' + Subkeys[I], 'DisplayName', DisplayName) and
      (CompareText(DisplayName, '{#LegacyAppName}') = 0) then
    begin
      if not ReadUninstallCommand(RootKey, Subkeys[I], CommandLine) then
      begin
        Result := 'Setup found an existing {#LegacyAppName} installation, but could not find its uninstaller.';
        Exit;
      end;

      WizardForm.StatusLabel.Caption := 'Removing previous {#LegacyAppName} installation...';
      WizardForm.ProgressGauge.Style := npbstMarquee;

      if not ExecuteCommandLine(CommandLine, ResultCode) then
      begin
        Result := 'Setup could not start the previous {#LegacyAppName} uninstaller.';
        Exit;
      end;

      if (ResultCode <> 0) and (ResultCode <> 3010) then
      begin
        Result := 'Previous {#LegacyAppName} uninstall failed with exit code ' + IntToStr(ResultCode) + '.';
        Exit;
      end;
    end;
  end;
end;

function RemoveLegacyInstallations(): String;
begin
  Result := UninstallLegacyFromRoot(HKCU);

  if Result = '' then
  begin
    Result := UninstallLegacyFromRoot(HKLM32);
  end;

  if Result = '' then
  begin
    Result := UninstallLegacyFromRoot(HKLM64);
  end;
end;

#if SkipPrerequisites != "true"
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
  Result := RemoveLegacyInstallations();

  if Result <> '' then
  begin
    Exit;
  end;

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
#else
function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := RemoveLegacyInstallations();
end;
#endif
