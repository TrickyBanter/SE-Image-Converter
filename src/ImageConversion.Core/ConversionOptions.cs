using SkiaSharp;

namespace ImageConversion.Core;

public sealed record ConversionOptions
{
    public PanelPreset PanelPreset { get; init; } = PanelPreset.Defaults[0];

    public ResizeMode ResizeMode { get; init; } = ResizeMode.Fit;

    public DitheringMode DitheringMode { get; init; } = DitheringMode.FloydSteinberg;

    public bool MaintainAspectRatio { get; init; } = true;

    public bool PreserveTransparency { get; init; } = true;

    public SKColor BackgroundColor { get; init; } = SKColors.Black;
}
