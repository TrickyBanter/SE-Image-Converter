namespace ImageConversion.Core;

/// <summary>
/// Coordinates for the bodies in Space Engineers' vanilla Star System scenario.
/// Positions are body centres in the Space Engineers GPS coordinate system.
/// </summary>
public static class StarSystemMap
{
    public static IReadOnlyList<StarSystemBody> VanillaBodies { get; } =
    [
        new("Earthlike", new JumpDriveVector(0, 0, 0), StarSystemBodyKind.Planet),
        new("Moon", new JumpDriveVector(16_384, 136_384, -113_616), StarSystemBodyKind.Moon),
        new("Mars", new JumpDriveVector(1_031_072, 131_072, 1_631_072), StarSystemBodyKind.Planet),
        new("Europa", new JumpDriveVector(916_384, 16_384, 1_616_384), StarSystemBodyKind.Moon),
        new("Alien", new JumpDriveVector(131_072, 131_072, 5_731_072), StarSystemBodyKind.Planet),
        new("Titan", new JumpDriveVector(36_384, 226_384, 5_796_384), StarSystemBodyKind.Moon),
        new("Triton", new JumpDriveVector(-284_464, -2_434_464, 365_536), StarSystemBodyKind.Planet),
        new("Pertam", new JumpDriveVector(-3_967_232, -32_232, -767_232), StarSystemBodyKind.Planet),
    ];
}

public sealed record StarSystemBody(string Name, JumpDriveVector Position, StarSystemBodyKind Kind);

public enum StarSystemBodyKind
{
    Planet,
    Moon,
}
