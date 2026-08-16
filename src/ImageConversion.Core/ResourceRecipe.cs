namespace ImageConversion.Core;

public sealed record ResourceRecipe(
    string Id,
    string Name,
    IReadOnlyList<SpaceEngineersBlockQuantity> Blocks);
