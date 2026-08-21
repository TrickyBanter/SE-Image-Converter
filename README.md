# SE Toolkit

Toolkit for Space Engineers LCD art, jump planning, and resource calculations.

This is a modern WinUI 3 desktop app that turns ordinary image files into
paste-ready Space Engineers LCD/Text Panel Monospace text. Version 2.0.0 also
adds Space Engineers planning tools for jump drive routing and block build
resources, making the app a broader toolkit rather than only an image converter.

<img width="1236" height="1033" alt="SE Toolkit app window" src="https://github.com/user-attachments/assets/901d998c-7acf-4633-8bc8-1fee7a4bdaf9" />

## For users

### Download

Download the latest installer or portable zip from the
[latest GitHub release](https://github.com/TrickyBanter/SE-Toolkit/releases/latest).

Use the setup `.exe` for a normal install. Use the portable `.zip` if you want
to run the app without installing it.

The setup executable is currently unsigned. Windows SmartScreen may warn until
the app is signed and has reputation.

### Space Engineers usage

Use the side menu to switch between the Image Converter, Jump Drive Calculator,
Resource Calculator, and Settings.

### Image converter

1. Open an image.
2. Choose the LCD/Text Panel type, resize mode, dithering mode, and transparency options.
3. Convert and copy the generated string.
4. In Space Engineers, set the LCD content to `Text and Images`.
5. Paste the string, select `Monospace`, and start with font size `0.1`.

### Updates

The app checks GitHub Releases on startup and from the in-app update controls.
The in-app updater downloads installer `.exe` releases. Portable `.zip` releases
are intended for manual downloads.

### Jump drive calculator

Use the Jump Drive Calculator tab to paste Space Engineers GPS coordinates or
enter X/Y/Z positions manually. Pick standard or Prototech jump drives, set the
drive count and ship mass, then calculate distance, range, required jumps, travel
time, and leg-by-leg route details.

### Resource calculator

Use the Resource Calculator tab to search the bundled vanilla block catalog, add
small-grid or large-grid block variants with quantities, and total the components
needed to build them. You can also save the current block list as a local recipe,
then add that recipe to a calculation with a quantity such as five missiles.

## For developers

### Projects

- `src/ImageConversion.Core` contains the deterministic conversion engine.
- `src/ImageConversion.App` contains the WinUI 3 desktop app.
- `tests/ImageConversion.Core.Tests` contains focused conversion tests.

### Requirements

- Windows 10 1809 or newer.
- .NET 10 SDK.
- WinUI 3 templates / Windows App SDK tooling.

Install the WinUI templates once after the SDK is installed:

```powershell
dotnet new install Microsoft.WindowsAppSDK.WinUI.CSharp.Templates
```

### Build and test

```powershell
dotnet restore .\SEToolkit.slnx
dotnet build .\SEToolkit.slnx
dotnet test .\SEToolkit.slnx
```

### Run locally

```powershell
dotnet run --project .\src\ImageConversion.App\ImageConversion.App.csproj
```

If you prefer to launch the built app directly:

```powershell
.\src\ImageConversion.App\bin\Debug\net10.0-windows10.0.19041.0\win-x64\SE Toolkit.exe
```

The app is currently configured as an unpackaged, framework-dependent WinUI app.
Release builds require the .NET Desktop Runtime and Windows App Runtime to be
installed on the target machine.

### Publishing a release

GitHub Releases are the production distribution channel. Each release should
include:

- `SEToolkit-Setup-<version>.exe` for normal installation.
- `SEToolkit-Portable-<version>-win-x64.zip` for portable use.

To build release artifacts:

```powershell
.\build\Release.ps1 -Version 2.0.0
```

The script runs the Release test suite, publishes a framework-dependent
`win-x64` build, creates the portable zip, and builds the setup executable with
Inno Setup 6 or newer. Install Inno Setup first, or pass its compiler path
explicitly:

```powershell
.\build\Release.ps1 -Version 2.0.0 -InnoSetupCompiler "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

For a portable-only build:

```powershell
.\build\Release.ps1 -Version 2.0.0 -SkipInstaller
```

Before publishing a release, update the app version metadata in
`src/ImageConversion.App/ImageConversion.App.csproj`. Then create a GitHub
release tag such as `v2.0.0` and upload both files from `artifacts/release`.

### Update behavior

Release tags should be semantic versions such as `v2.0.0` or `2.0.0`. The
in-app updater downloads the setup `.exe` asset; portable `.zip` assets are
ignored by the updater and are intended for manual downloads.
