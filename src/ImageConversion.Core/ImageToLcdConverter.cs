using System.Text;
using SkiaSharp;

namespace ImageConversion.Core;

public sealed class ImageToLcdConverter
{
    public ConversionResult Convert(byte[] imageData, ConversionOptions options)
    {
        ArgumentNullException.ThrowIfNull(imageData);
        ArgumentNullException.ThrowIfNull(options);

        using SKBitmap? decoded = SKBitmap.Decode(imageData);
        if (decoded is null)
        {
            throw new InvalidOperationException("The selected file could not be decoded as an image.");
        }

        return Convert(decoded, options);
    }

    public ConversionResult Convert(SKBitmap source, ConversionOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);

        using SKBitmap canvas = ResizeToPanel(source, options);
        PixelBuffer buffer = PixelBuffer.FromBitmap(canvas, options);

        return options.DitheringMode switch
        {
            DitheringMode.FloydSteinberg => ConvertWithErrorDiffusion(buffer, ErrorDiffusionKernel.FloydSteinberg),
            DitheringMode.Atkinson => ConvertWithErrorDiffusion(buffer, ErrorDiffusionKernel.Atkinson),
            DitheringMode.SierraLite => ConvertWithErrorDiffusion(buffer, ErrorDiffusionKernel.SierraLite),
            DitheringMode.Stucki => ConvertWithErrorDiffusion(buffer, ErrorDiffusionKernel.Stucki),
            DitheringMode.Burkes => ConvertWithErrorDiffusion(buffer, ErrorDiffusionKernel.Burkes),
            DitheringMode.OrderedBayer2 => ConvertWithOrderedDither(buffer, BayerMatrices.Size2),
            DitheringMode.OrderedBayer4 => ConvertWithOrderedDither(buffer, BayerMatrices.Size4),
            DitheringMode.OrderedBayer8 => ConvertWithOrderedDither(buffer, BayerMatrices.Size8),
            _ => ConvertNearest(buffer),
        };
    }

    private static SKBitmap ResizeToPanel(SKBitmap source, ConversionOptions options)
    {
        int width = options.PanelPreset.CharacterWidth;
        int height = options.PanelPreset.CharacterHeight;
        SKImageInfo targetInfo = new(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        SKBitmap target = new(targetInfo);

        using SKCanvas canvas = new(target);
        canvas.Clear(options.PreserveTransparency ? SKColors.Transparent : options.BackgroundColor);

        SKRect sourceRect = CalculateSourceRect(source.Width, source.Height, options);
        SKRect destinationRect = CalculateDestinationRect(source.Width, source.Height, width, height, options);

        using SKPaint paint = new()
        {
            IsAntialias = false,
        };

        canvas.DrawBitmap(
            source,
            sourceRect,
            destinationRect,
            new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None),
            paint);
        canvas.Flush();
        return target;
    }

    private static SKRect CalculateSourceRect(int sourceWidth, int sourceHeight, ConversionOptions options)
    {
        if (!options.MaintainAspectRatio || options.ResizeMode is not ResizeMode.Fill)
        {
            return new SKRect(0, 0, sourceWidth, sourceHeight);
        }

        double targetAspect = options.PanelPreset.CharacterWidth / (double)options.PanelPreset.CharacterHeight;
        double sourceAspect = sourceWidth / (double)sourceHeight;

        if (sourceAspect > targetAspect)
        {
            float cropWidth = (float)(sourceHeight * targetAspect);
            float left = (sourceWidth - cropWidth) / 2.0f;
            return new SKRect(left, 0, left + cropWidth, sourceHeight);
        }

        float cropHeight = (float)(sourceWidth / targetAspect);
        float top = (sourceHeight - cropHeight) / 2.0f;
        return new SKRect(0, top, sourceWidth, top + cropHeight);
    }

    private static SKRect CalculateDestinationRect(
        int sourceWidth,
        int sourceHeight,
        int targetWidth,
        int targetHeight,
        ConversionOptions options)
    {
        if (!options.MaintainAspectRatio || options.ResizeMode is ResizeMode.Stretch or ResizeMode.Fill)
        {
            return new SKRect(0, 0, targetWidth, targetHeight);
        }

        double scale = Math.Min(targetWidth / (double)sourceWidth, targetHeight / (double)sourceHeight);
        float width = (float)(sourceWidth * scale);
        float height = (float)(sourceHeight * scale);
        float left = (targetWidth - width) / 2.0f;
        float top = (targetHeight - height) / 2.0f;

        return new SKRect(left, top, left + width, top + height);
    }

    private static ConversionResult ConvertNearest(PixelBuffer buffer)
    {
        SKBitmap preview = CreatePreviewBitmap(buffer.Width, buffer.Height);
        StringBuilder builder = new(buffer.Width * buffer.Height + buffer.Height);

        for (int y = 0; y < buffer.Height; y++)
        {
            AppendLineBreak(builder, y);

            for (int x = 0; x < buffer.Width; x++)
            {
                if (buffer.IsTransparent(x, y))
                {
                    builder.Append(' ');
                    preview.SetPixel(x, y, SKColors.Transparent);
                    continue;
                }

                RgbColor current = buffer.Get(x, y);
                QuantizedColor quantized = SpaceEngineersGlyphPalette.Quantize(current.Red, current.Green, current.Blue);
                builder.Append(quantized.Glyph);
                preview.SetPixel(x, y, new SKColor((byte)quantized.Red, (byte)quantized.Green, (byte)quantized.Blue));
            }
        }

        return CreateResult(builder.ToString(), buffer.Width, buffer.Height, preview);
    }

    private static ConversionResult ConvertWithOrderedDither(PixelBuffer buffer, int[,] matrix)
    {
        int matrixSize = matrix.GetLength(0);
        double denominator = matrixSize * matrixSize;
        SKBitmap preview = CreatePreviewBitmap(buffer.Width, buffer.Height);
        StringBuilder builder = new(buffer.Width * buffer.Height + buffer.Height);

        for (int y = 0; y < buffer.Height; y++)
        {
            AppendLineBreak(builder, y);

            for (int x = 0; x < buffer.Width; x++)
            {
                if (buffer.IsTransparent(x, y))
                {
                    builder.Append(' ');
                    preview.SetPixel(x, y, SKColors.Transparent);
                    continue;
                }

                double threshold = ((matrix[y % matrixSize, x % matrixSize] + 0.5) / denominator - 0.5) * (255.0 / 7.0);
                RgbColor current = buffer.Get(x, y);
                QuantizedColor quantized = SpaceEngineersGlyphPalette.Quantize(
                    current.Red + threshold,
                    current.Green + threshold,
                    current.Blue + threshold);

                builder.Append(quantized.Glyph);
                preview.SetPixel(x, y, new SKColor((byte)quantized.Red, (byte)quantized.Green, (byte)quantized.Blue));
            }
        }

        return CreateResult(builder.ToString(), buffer.Width, buffer.Height, preview);
    }

    private static ConversionResult ConvertWithErrorDiffusion(PixelBuffer buffer, ErrorDiffusionKernel kernel)
    {
        RgbColor[,] working = buffer.ClonePixels();
        SKBitmap preview = CreatePreviewBitmap(buffer.Width, buffer.Height);
        StringBuilder builder = new(buffer.Width * buffer.Height + buffer.Height);

        for (int y = 0; y < buffer.Height; y++)
        {
            AppendLineBreak(builder, y);

            for (int x = 0; x < buffer.Width; x++)
            {
                if (buffer.IsTransparent(x, y))
                {
                    builder.Append(' ');
                    preview.SetPixel(x, y, SKColors.Transparent);
                    continue;
                }

                RgbColor current = working[x, y];
                QuantizedColor quantized = SpaceEngineersGlyphPalette.Quantize(current.Red, current.Green, current.Blue);

                builder.Append(quantized.Glyph);
                preview.SetPixel(x, y, new SKColor((byte)quantized.Red, (byte)quantized.Green, (byte)quantized.Blue));

                double errorRed = current.Red - quantized.Red;
                double errorGreen = current.Green - quantized.Green;
                double errorBlue = current.Blue - quantized.Blue;

                foreach (ErrorDiffusionWeight weight in kernel.Weights)
                {
                    int targetX = x + weight.OffsetX;
                    int targetY = y + weight.OffsetY;

                    if (targetX < 0 ||
                        targetX >= buffer.Width ||
                        targetY < 0 ||
                        targetY >= buffer.Height ||
                        buffer.IsTransparent(targetX, targetY))
                    {
                        continue;
                    }

                    RgbColor target = working[targetX, targetY];
                    working[targetX, targetY] = new RgbColor(
                        target.Red + errorRed * weight.Factor,
                        target.Green + errorGreen * weight.Factor,
                        target.Blue + errorBlue * weight.Factor);
                }
            }
        }

        return CreateResult(builder.ToString(), buffer.Width, buffer.Height, preview);
    }

    private static void AppendLineBreak(StringBuilder builder, int y)
    {
        if (y > 0)
        {
            builder.AppendLine();
        }
    }

    private static SKBitmap CreatePreviewBitmap(int width, int height) =>
        new(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));

    private static ConversionResult CreateResult(string text, int width, int height, SKBitmap preview)
    {
        using (preview)
        using (SKImage image = SKImage.FromBitmap(preview))
        using (SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100))
        {
            return new ConversionResult(text, width, height, text.Length, encoded.ToArray());
        }
    }
}

internal readonly record struct RgbColor(double Red, double Green, double Blue);

internal sealed class PixelBuffer
{
    private readonly RgbColor[,] pixels;
    private readonly bool[,] transparent;

    private PixelBuffer(int width, int height)
    {
        Width = width;
        Height = height;
        pixels = new RgbColor[width, height];
        transparent = new bool[width, height];
    }

    public int Width { get; }

    public int Height { get; }

    public static PixelBuffer FromBitmap(SKBitmap bitmap, ConversionOptions options)
    {
        PixelBuffer buffer = new(bitmap.Width, bitmap.Height);

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                SKColor color = bitmap.GetPixel(x, y);

                if (options.PreserveTransparency && color.Alpha < 128)
                {
                    buffer.transparent[x, y] = true;
                    buffer.pixels[x, y] = new RgbColor(0, 0, 0);
                    continue;
                }

                if (!options.PreserveTransparency && color.Alpha < 255)
                {
                    double alpha = color.Alpha / 255.0;
                    buffer.pixels[x, y] = new RgbColor(
                        (color.Red * alpha) + (options.BackgroundColor.Red * (1.0 - alpha)),
                        (color.Green * alpha) + (options.BackgroundColor.Green * (1.0 - alpha)),
                        (color.Blue * alpha) + (options.BackgroundColor.Blue * (1.0 - alpha)));
                    continue;
                }

                buffer.pixels[x, y] = new RgbColor(color.Red, color.Green, color.Blue);
            }
        }

        return buffer;
    }

    public RgbColor Get(int x, int y) => pixels[x, y];

    public bool IsTransparent(int x, int y) => transparent[x, y];

    public RgbColor[,] ClonePixels() => (RgbColor[,])pixels.Clone();
}

internal sealed record ErrorDiffusionKernel(IReadOnlyList<ErrorDiffusionWeight> Weights)
{
    public static ErrorDiffusionKernel FloydSteinberg { get; } = new(
    [
        new(1, 0, 7.0 / 16.0),
        new(-1, 1, 3.0 / 16.0),
        new(0, 1, 5.0 / 16.0),
        new(1, 1, 1.0 / 16.0),
    ]);

    public static ErrorDiffusionKernel Atkinson { get; } = new(
    [
        new(1, 0, 1.0 / 8.0),
        new(2, 0, 1.0 / 8.0),
        new(-1, 1, 1.0 / 8.0),
        new(0, 1, 1.0 / 8.0),
        new(1, 1, 1.0 / 8.0),
        new(0, 2, 1.0 / 8.0),
    ]);

    public static ErrorDiffusionKernel SierraLite { get; } = new(
    [
        new(1, 0, 2.0 / 4.0),
        new(-1, 1, 1.0 / 4.0),
        new(0, 1, 1.0 / 4.0),
    ]);

    public static ErrorDiffusionKernel Stucki { get; } = new(
    [
        new(1, 0, 8.0 / 42.0),
        new(2, 0, 4.0 / 42.0),
        new(-2, 1, 2.0 / 42.0),
        new(-1, 1, 4.0 / 42.0),
        new(0, 1, 8.0 / 42.0),
        new(1, 1, 4.0 / 42.0),
        new(2, 1, 2.0 / 42.0),
        new(-2, 2, 1.0 / 42.0),
        new(-1, 2, 2.0 / 42.0),
        new(0, 2, 4.0 / 42.0),
        new(1, 2, 2.0 / 42.0),
        new(2, 2, 1.0 / 42.0),
    ]);

    public static ErrorDiffusionKernel Burkes { get; } = new(
    [
        new(1, 0, 8.0 / 32.0),
        new(2, 0, 4.0 / 32.0),
        new(-2, 1, 2.0 / 32.0),
        new(-1, 1, 4.0 / 32.0),
        new(0, 1, 8.0 / 32.0),
        new(1, 1, 4.0 / 32.0),
        new(2, 1, 2.0 / 32.0),
    ]);
}

internal sealed record ErrorDiffusionWeight(int OffsetX, int OffsetY, double Factor);

internal static class BayerMatrices
{
    public static int[,] Size2 { get; } =
    {
        { 0, 2 },
        { 3, 1 },
    };

    public static int[,] Size4 { get; } =
    {
        { 0, 8, 2, 10 },
        { 12, 4, 14, 6 },
        { 3, 11, 1, 9 },
        { 15, 7, 13, 5 },
    };

    public static int[,] Size8 { get; } =
    {
        { 0, 32, 8, 40, 2, 34, 10, 42 },
        { 48, 16, 56, 24, 50, 18, 58, 26 },
        { 12, 44, 4, 36, 14, 46, 6, 38 },
        { 60, 28, 52, 20, 62, 30, 54, 22 },
        { 3, 35, 11, 43, 1, 33, 9, 41 },
        { 51, 19, 59, 27, 49, 17, 57, 25 },
        { 15, 47, 7, 39, 13, 45, 5, 37 },
        { 63, 31, 55, 23, 61, 29, 53, 21 },
    };
}
