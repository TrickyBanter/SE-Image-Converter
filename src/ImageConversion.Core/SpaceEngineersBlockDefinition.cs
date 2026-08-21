namespace ImageConversion.Core;

public sealed record SpaceEngineersBlockDefinition(
    string Id,
    string DisplayName,
    string GridSize,
    string TypeId,
    string SubtypeId,
    IReadOnlyList<SpaceEngineersComponentRequirement> Components)
{
    public string DisplayLabel => $"{DisplayName} ({GridSize} Grid)";

    public string SearchText => $"{DisplayName} {GridSize} {TypeId} {SubtypeId}";
}
