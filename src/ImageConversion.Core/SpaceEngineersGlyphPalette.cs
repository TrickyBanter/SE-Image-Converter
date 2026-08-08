using SkiaSharp;

namespace ImageConversion.Core;

public static class SpaceEngineersGlyphPalette
{
    public const int FirstColorGlyph = 0xE100;

    public static char GetGlyph(int redLevel, int greenLevel, int blueLevel)
    {
        redLevel = Math.Clamp(redLevel, 0, 7);
        greenLevel = Math.Clamp(greenLevel, 0, 7);
        blueLevel = Math.Clamp(blueLevel, 0, 7);

        return (char)(FirstColorGlyph + (redLevel * 64) + (greenLevel * 8) + blueLevel);
    }

    public static QuantizedColor Quantize(double red, double green, double blue)
    {
        int redLevel = ToLevel(red);
        int greenLevel = ToLevel(green);
        int blueLevel = ToLevel(blue);

        return new QuantizedColor(
            redLevel,
            greenLevel,
            blueLevel,
            ToChannel(redLevel),
            ToChannel(greenLevel),
            ToChannel(blueLevel));
    }

    public static SKColor ToColor(int redLevel, int greenLevel, int blueLevel) =>
        new((byte)ToChannel(redLevel), (byte)ToChannel(greenLevel), (byte)ToChannel(blueLevel), 255);

    public static int ToLevel(double value) =>
        Math.Clamp((int)Math.Round(Math.Clamp(value, 0.0, 255.0) / 255.0 * 7.0), 0, 7);

    public static int ToChannel(int level) =>
        (int)Math.Round(Math.Clamp(level, 0, 7) / 7.0 * 255.0);
}

public readonly record struct QuantizedColor(
    int RedLevel,
    int GreenLevel,
    int BlueLevel,
    int Red,
    int Green,
    int Blue)
{
    public char Glyph => SpaceEngineersGlyphPalette.GetGlyph(RedLevel, GreenLevel, BlueLevel);
}
