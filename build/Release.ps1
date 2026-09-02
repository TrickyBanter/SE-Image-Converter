[CmdletBinding()]
param(
    [string]$Version,
    [string]$RuntimeIdentifier = "win-x64",
    [string]$Configuration = "Release",
    [string]$InnoSetupCompiler,
    [string]$DotNetDesktopRuntimeInstaller,
    [string]$WindowsAppRuntimeInstaller,
    [switch]$SkipPrerequisites,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE."
    }
}

function Write-ChecksumManifest {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$ArtifactPaths,

        [Parameter(Mandatory = $true)]
        [string]$ManifestPath
    )

    $lines = $ArtifactPaths |
        Where-Object { Test-Path $_ } |
        ForEach-Object {
            $item = Get-Item -LiteralPath $_
            $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $item.FullName).Hash.ToLowerInvariant()
            "$hash  $($item.Name)"
        }

    Set-Content -LiteralPath $ManifestPath -Value $lines -Encoding ascii
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$appProject = Join-Path $repoRoot "src\ImageConversion.App\ImageConversion.App.csproj"
$solution = Join-Path $repoRoot "SEToolkit.slnx"
$publishRoot = Join-Path $repoRoot "artifacts\publish\SEToolkit\$RuntimeIdentifier"
$releaseRoot = Join-Path $repoRoot "artifacts\release"
$installerScript = Join-Path $repoRoot "build\SEToolkit.iss"
$prerequisitesRoot = Join-Path $repoRoot "build\prerequisites"
$checksumManifestPath = Join-Path $releaseRoot "SHA256SUMS.txt"

if ([string]::IsNullOrWhiteSpace($DotNetDesktopRuntimeInstaller)) {
    $DotNetDesktopRuntimeInstaller = Join-Path $prerequisitesRoot "windowsdesktop-runtime-win-x64.exe"
}

if ([string]::IsNullOrWhiteSpace($WindowsAppRuntimeInstaller)) {
    $WindowsAppRuntimeInstaller = Join-Path $prerequisitesRoot "WindowsAppRuntimeInstall-x64.exe"
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$projectXml = Get-Content $appProject
    $Version = $projectXml.Project.PropertyGroup.Version
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Could not determine the app version. Pass -Version or set Version in $appProject."
}

if (-not $SkipInstaller -and -not $SkipPrerequisites) {
    if (-not (Test-Path $DotNetDesktopRuntimeInstaller)) {
        throw "Missing .NET Desktop Runtime installer: $DotNetDesktopRuntimeInstaller. Download the x64 .NET Desktop Runtime installer and save it there, pass -DotNetDesktopRuntimeInstaller, or use -SkipPrerequisites."
    }

    if (-not (Test-Path $WindowsAppRuntimeInstaller)) {
        throw "Missing Windows App Runtime installer: $WindowsAppRuntimeInstaller. Download WindowsAppRuntimeInstall-x64.exe and save it there, pass -WindowsAppRuntimeInstaller, or use -SkipPrerequisites."
    }
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

Invoke-NativeCommand "dotnet" @(
    "test",
    $solution,
    "--configuration",
    $Configuration
)

Invoke-NativeCommand "dotnet" @(
    "publish",
    $appProject,
    "--configuration",
    $Configuration,
    "--runtime",
    $RuntimeIdentifier,
    "--self-contained",
    "false",
    "-p:Version=$Version",
    "-p:AssemblyVersion=$assemblyVersion",
    "-p:FileVersion=$assemblyVersion",
    "-p:PackageVersion=$Version",
    "-p:PublishDir=$publishRoot\"
)

$zipPath = Join-Path $releaseRoot "SEToolkit-Portable-$Version-$RuntimeIdentifier.zip"
if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishRoot "*") -DestinationPath $zipPath -CompressionLevel Optimal

if ($SkipInstaller) {
    Write-ChecksumManifest @($zipPath) $checksumManifestPath

    Write-Host "Release artifacts:"
    Write-Host "  Portable:  $zipPath"
    Write-Host "  Checksums: $checksumManifestPath"
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

$innoArguments = @(
    "/DAppVersion=$Version",
    "/DPublishDir=$publishRoot",
    "/DOutputDir=$releaseRoot"
)

if ($SkipPrerequisites) {
    $innoArguments += "/DSkipPrerequisites=true"
} else {
    $innoArguments += "/DDotNetDesktopRuntimeInstaller=$DotNetDesktopRuntimeInstaller"
    $innoArguments += "/DWindowsAppRuntimeInstaller=$WindowsAppRuntimeInstaller"
}

$innoArguments += $installerScript

Invoke-NativeCommand $InnoSetupCompiler $innoArguments

$installerPath = Join-Path $releaseRoot "SEToolkit-Setup-$Version.exe"
Write-ChecksumManifest @($installerPath, $zipPath) $checksumManifestPath

Write-Host "Release artifacts:"
Write-Host "  Installer: $installerPath"
Write-Host "  Portable:  $zipPath"
Write-Host "  Checksums: $checksumManifestPath"
