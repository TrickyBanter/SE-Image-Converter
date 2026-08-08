namespace ImageConversion.Core;

public sealed record PanelPreset(
    string Name,
    int CharacterWidth,
    int CharacterHeight,
    double AspectRatio,
    string Size,
    string Dlc)
{
    public override string ToString() => $"{Name} - {Size} ({CharacterWidth}x{CharacterHeight}){DlcSuffix()}";

    public static IReadOnlyList<PanelPreset> Defaults { get; } =
    [
        new("LCD Panel", 178, 178, 1.0, "SG 3x3, LG 1x1", "Vanilla"),
        new("Text Panel - Small Grid", 88, 88, 1.0, "SG 1x1", "Vanilla"),
        new("Text Panel - Large Grid", 178, 100, 1.78, "LG 1x1", "Vanilla"),
        new("Wide LCD Panel", 178, 89, 2.0, "SG 6x3, LG 2x1", "Vanilla"),

        new("Corner LCD Top", 178, 22, 8.0, "SG/LG 1x1x1 top eighth", "Vanilla"),
        new("Corner LCD Bottom", 178, 22, 8.0, "SG/LG 1x1x1 bottom eighth", "Vanilla"),
        new("Corner LCD Flat Top", 178, 22, 8.0, "SG/LG 1x1x1 top eighth", "Vanilla"),
        new("Corner LCD Flat Bottom", 178, 22, 8.0, "SG/LG 1x1x1 bottom eighth", "Vanilla"),

        new("Sci-Fi LCD Panel 5x5", 178, 178, 1.0, "LG 5x5", "DLC"),
        new("Sci-Fi LCD Panel 3x5", 107, 178, 0.6, "LG 3x5", "DLC"),
        new("Sci-Fi LCD Panel 3x3", 178, 178, 1.0, "LG 3x3", "DLC"),
        new("Transparent LCD - Large Grid", 178, 178, 1.0, "LG 1x1", "DLC"),
        new("Transparent LCD - Small Grid", 88, 88, 1.0, "SG 1x1", "DLC"),
        new("Holo LCD", 178, 45, 4.0, "LG 1x1", "DLC"),
        new("Inset LCD Panel", 178, 178, 1.0, "LG 1x1", "DLC"),
        new("Sloped LCD Panel", 178, 178, 1.0, "LG 1x1", "DLC"),
        new("Curved LCD Panel", 178, 178, 1.0, "LG 1x1", "DLC"),
    ];

    private string DlcSuffix() => Dlc.Equals("Vanilla", StringComparison.OrdinalIgnoreCase)
        ? string.Empty
        : $" - {Dlc}";
}
