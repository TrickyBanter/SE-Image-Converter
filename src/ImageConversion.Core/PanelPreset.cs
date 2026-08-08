namespace ImageConversion.Core;

public sealed record PanelPreset(
    string Name,
    int CharacterWidth,
    int CharacterHeight,
    double AspectRatio)
{
    public override string ToString() => $"{Name} ({CharacterWidth}x{CharacterHeight})";

    public static IReadOnlyList<PanelPreset> Defaults { get; } =
    [
        new("LCD Panel / Square", 178, 178, 1.0),
        new("Wide LCD Panel", 178, 89, 2.0),
        new("Text Panel", 178, 100, 1.78),
        new("Small Text Panel", 88, 88, 1.0),
        new("Corner LCD", 126, 126, 1.0),
        new("Keyboard / Cockpit Surface", 80, 45, 16.0 / 9.0),
    ];
}
