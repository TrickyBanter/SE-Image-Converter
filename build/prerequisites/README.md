# Installer prerequisites

Place the redistributable runtime installers used by the Inno Setup release build in this folder:

- `windowsdesktop-runtime-win-x64.exe`
- `WindowsAppRuntimeInstall-x64.exe`

`build\Release.ps1` embeds these files into `SEToolkit-Setup-<version>.exe` by default. During setup, the installer checks for the required .NET Desktop Runtime and Windows App Runtime, then runs the bundled installer only when the runtime is missing.

Use `-DotNetDesktopRuntimeInstaller` or `-WindowsAppRuntimeInstaller` to point at installers stored elsewhere. Use `-SkipPrerequisites` to build an installer without bundled runtime installers.
