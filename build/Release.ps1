[CmdletBinding()]
param(
    [string]$Version,
    [string]$RuntimeIdentifier = "win-x64",
    [string]$Configuration = "Release",
    [string]$InnoSetupCompiler,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$appProject = Join-Path $repoRoot "src\ImageConversion.App\ImageConversion.App.csproj"
$solution = Join-Path $repoRoot "SEImageConverter.slnx"
$publishRoot = Join-Path $repoRoot "artifacts\publish\SEImageConverter\$RuntimeIdentifier"
$releaseRoot = Join-Path $repoRoot "artifacts\release"
$installerScript = Join-Path $repoRoot "build\SEImageConverter.iss"

if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$projectXml = Get-Content $appProject
    $Version = $projectXml.Project.PropertyGroup.Version
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Could not determine the app version. Pass -Version or set Version in $appProject."
}

$parsedVersion = [version]$Version
$assemblyVersion = [version]::new(
    $parsedVersion.Major,
    $parsedVersion.Minor,
    [Math]::Max($parsedVersion.Build, 0),
    [Math]::Max($parsedVersion.Revision, 0))

if (Test-Path $publishRoot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishRoot, $releaseRoot | Out-Null

dotnet test $solution --configuration $Configuration

dotnet publish $appProject `
    --configuration $Configuration `
    --runtime $RuntimeIdentifier `
    --self-contained false `
    -p:Version=$Version `
    -p:AssemblyVersion=$assemblyVersion `
    -p:FileVersion=$assemblyVersion `
    -p:PackageVersion=$Version `
    -p:PublishDir="$publishRoot\"

$zipPath = Join-Path $releaseRoot "SEImageConverter-Portable-$Version-$RuntimeIdentifier.zip"
if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishRoot "*") -DestinationPath $zipPath -CompressionLevel Optimal

if ($SkipInstaller) {
    Write-Host "Release artifacts:"
    Write-Host "  Portable:  $zipPath"
    exit 0
}

if ([string]::IsNullOrWhiteSpace($InnoSetupCompiler)) {
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 7\ISCC.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 7\ISCC.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 7\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    )

    $InnoSetupCompiler = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($InnoSetupCompiler)) {
        $command = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
        if ($command) {
            $InnoSetupCompiler = $command.Source
        }
    }
}

if ([string]::IsNullOrWhiteSpace($InnoSetupCompiler) -or -not (Test-Path $InnoSetupCompiler)) {
    throw "Inno Setup compiler was not found. Install Inno Setup 6 or newer, pass -InnoSetupCompiler C:\Path\To\ISCC.exe, or use -SkipInstaller for a portable-only build."
}

& $InnoSetupCompiler `
    "/DAppVersion=$Version" `
    "/DPublishDir=$publishRoot" `
    "/DOutputDir=$releaseRoot" `
    $installerScript

Write-Host "Release artifacts:"
Write-Host "  Installer: $(Join-Path $releaseRoot "SEImageConverter-Setup-$Version.exe")"
Write-Host "  Portable:  $zipPath"
