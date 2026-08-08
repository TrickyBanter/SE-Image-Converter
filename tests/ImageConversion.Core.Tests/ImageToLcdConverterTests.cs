using ImageConversion.Core;
using SkiaSharp;
using Xunit;

namespace ImageConversion.Core.Tests;

public sealed class ImageToLcdConverterTests
{
    private static readonly PanelPreset TinySquare = new("Tiny square", 2, 2, 1, "Test", "Vanilla");
    private static readonly PanelPreset TinyWide = new("Tiny wide", 4, 2, 2, "Test", "Vanilla");

    [Fact]
    public void TransparentPixelsBecomeSpacesWhenTransparencyIsPreserved()
    {
        using SKBitmap bitmap = new(new SKImageInfo(2, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        bitmap.SetPixel(0, 0, SKColors.Red);
        bitmap.SetPixel(1, 0, SKColors.Transparent);

        ConversionResult result = Convert(bitmap, new ConversionOptions
        {
            PanelPreset = new PanelPreset("One row", 2, 1, 2, "Test", "Vanilla"),
            DitheringMode = DitheringMode.None,
            PreserveTransparency = true,
            MaintainAspectRatio = false,
        });

        Assert.Equal(2, result.Text.Length);
        Assert.NotEqual(' ', result.Text[0]);
        Assert.Equal(' ', result.Text[1]);
    }

    [Fact]
    public void TransparentPixelsCompositeAgainstBackgroundWhenTransparencyIsDisabled()
    {
        using SKBitmap bitmap = new(new SKImageInfo(1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        bitmap.SetPixel(0, 0, SKColors.Transparent);

        ConversionResult result = Convert(bitmap, new ConversionOptions
        {
            PanelPreset = new PanelPreset("Single", 1, 1, 1, "Test", "Vanilla"),
            DitheringMode = DitheringMode.None,
            PreserveTransparency = false,
            MaintainAspectRatio = false,
            BackgroundColor = SKColors.White,
        });

        Assert.NotEqual(" ", result.Text);
        Assert.Equal(SpaceEngineersGlyphPalette.GetGlyph(7, 7, 7), result.Text[0]);
    }

    [Fact]
    public void MaintainsAspectRatioByLetterboxing()
    {
        using SKBitmap bitmap = new(new SKImageInfo(4, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        bitmap.Erase(SKColors.White);

        ConversionResult result = Convert(bitmap, new ConversionOptions
        {
            PanelPreset = TinySquare,
            DitheringMode = DitheringMode.None,
            PreserveTransparency = true,
            MaintainAspectRatio = true,
            ResizeMode = ResizeMode.Fit,
        });

        string[] lines = result.Text.Split(Environment.NewLine);
        Assert.Equal(2, lines.Length);
        Assert.Contains(' ', result.Text);
        Assert.All(lines, line => Assert.Equal(2, line.Length));
    }

    [Fact]
    public void StretchFillsTargetWhenAspectRatioIsNotMaintained()
    {
        using SKBitmap bitmap = new(new SKImageInfo(4, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        bitmap.Erase(SKColors.White);

        ConversionResult result = Convert(bitmap, new ConversionOptions
        {
            PanelPreset = TinySquare,
            DitheringMode = DitheringMode.None,
            PreserveTransparency = true,
            MaintainAspectRatio = false,
            ResizeMode = ResizeMode.Stretch,
        });

        Assert.DoesNotContain(' ', result.Text);
    }

    [Theory]
    [InlineData(DitheringMode.None)]
    [InlineData(DitheringMode.FloydSteinberg)]
    [InlineData(DitheringMode.Atkinson)]
    [InlineData(DitheringMode.OrderedBayer2)]
    [InlineData(DitheringMode.OrderedBayer4)]
    public void DitheringModesPreserveOutputDimensions(DitheringMode mode)
    {
        using SKBitmap bitmap = new(new SKImageInfo(4, 4, SKColorType.Rgba8888, SKAlphaType.Unpremul));

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                bitmap.SetPixel(x, y, new SKColor((byte)(x * 64), (byte)(y * 64), 128));
            }
        }

        ConversionResult result = Convert(bitmap, new ConversionOptions
        {
            PanelPreset = TinyWide,
            DitheringMode = mode,
            PreserveTransparency = true,
            MaintainAspectRatio = false,
        });

        Assert.Equal(4, result.Width);
        Assert.Equal(2, result.Height);
        Assert.Equal(2, result.Text.Split(Environment.NewLine).Length);
    }

    [Fact]
    public void DeterministicTinyImageProducesExpectedGlyphsAndLineBreaks()
    {
        using SKBitmap bitmap = new(new SKImageInfo(2, 2, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        bitmap.SetPixel(0, 0, SKColors.Black);
        bitmap.SetPixel(1, 0, SKColors.White);
        bitmap.SetPixel(0, 1, SKColors.Red);
        bitmap.SetPixel(1, 1, SKColors.Blue);

        ConversionResult result = Convert(bitmap, new ConversionOptions
        {
            PanelPreset = TinySquare,
            DitheringMode = DitheringMode.None,
            PreserveTransparency = true,
            MaintainAspectRatio = false,
        });

        string expected =
            $"{SpaceEngineersGlyphPalette.GetGlyph(0, 0, 0)}{SpaceEngineersGlyphPalette.GetGlyph(7, 7, 7)}{Environment.NewLine}" +
            $"{SpaceEngineersGlyphPalette.GetGlyph(7, 0, 0)}{SpaceEngineersGlyphPalette.GetGlyph(0, 0, 7)}";

        Assert.Equal(expected, result.Text);
    }

    private static ConversionResult Convert(SKBitmap bitmap, ConversionOptions options) =>
        new ImageToLcdConverter().Convert(bitmap, options);
}
