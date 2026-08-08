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
dotnet test .\tests\ImageConversion.Core.Tests\ImageConversion.Core.Tests.csproj
```

## Space Engineers usage

1. Open an image.
2. Choose the LCD/Text Panel type, resize mode, dithering mode, and transparency options.
3. Convert and copy the generated string.
4. In Space Engineers, set the LCD content to `Text and Images`.
5. Paste the string, select `Monospace`, and start with font size `0.1`.
