# SE Image Converter
Image converter for use in Space Engineers.

This is a modern WinUI 3 desktop app that converts ordinary image files into
paste-ready Space Engineers LCD/Text Panel Monospace text.

## Projects

- `src/ImageConversion.Core` contains the deterministic conversion engine.
- `src/ImageConversion.App` contains the WinUI 3 desktop app.
- `tests/ImageConversion.Core.Tests` contains focused conversion tests.

## Requirements

- Windows 10 1809 or newer.
- .NET 10 SDK.
- WinUI 3 templates / Windows App SDK tooling.

Install the WinUI templates once after the SDK is installed:

```powershell
dotnet new install Microsoft.WindowsAppSDK.WinUI.CSharp.Templates
```

## Build and test

```powershell
dotnet restore .\SEImageConverter.slnx
dotnet build .\SEImageConverter.slnx
dotnet test .\SEImageConverter.slnx
```

## Run

```powershell
dotnet run --project .\src\ImageConversion.App\ImageConversion.App.csproj
```

If you prefer to launch the built app directly:

```powershell
.\src\ImageConversion.App\bin\Debug\net10.0-windows10.0.19041.0\win-x64\SE Image Converter.exe
```

The app is currently configured as an unpackaged, framework-dependent WinUI app.
Release builds require the .NET Desktop Runtime and Windows App Runtime to be
installed on the target machine.

## Release

GitHub Releases are the production distribution channel. Each release should
include:

- `SEImageConverter-Setup-<version>.exe` for normal installation.
- `SEImageConverter-Portable-<version>-win-x64.zip` for portable use.

The setup executable is currently unsigned. Windows SmartScreen may warn users
until the app is signed and has reputation.

To build release artifacts:

```powershell
.\build\Release.ps1 -Version 1.0.0
```

The script runs the Release test suite, publishes a framework-dependent
`win-x64` build, creates the portable zip, and builds the setup executable with
Inno Setup 6 or newer. Install Inno Setup first, or pass its compiler path
explicitly:

```powershell
.\build\Release.ps1 -Version 1.0.0 -InnoSetupCompiler "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

For a portable-only build:

```powershell
.\build\Release.ps1 -Version 1.0.0 -SkipInstaller
```

Before publishing a release, update the app version metadata in
`src/ImageConversion.App/ImageConversion.App.csproj`. Then create a GitHub
release tag such as `v1.0.0` and upload both files from `artifacts/release`.

## Updates

The app checks GitHub Releases on startup and from the in-app update controls.
Release tags should be semantic versions such as `v1.2.3` or `1.2.3`. The
in-app updater downloads the setup `.exe` asset; portable `.zip` assets are
ignored by the updater and are intended for manual downloads.

## Space Engineers usage

1. Open an image.
2. Choose the LCD/Text Panel type, resize mode, dithering mode, and transparency options.
3. Convert and copy the generated string.
4. In Space Engineers, set the LCD content to `Text and Images`.
5. Paste the string, select `Monospace`, and start with font size `0.1`.
