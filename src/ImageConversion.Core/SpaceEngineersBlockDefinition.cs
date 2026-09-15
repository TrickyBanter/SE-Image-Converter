namespace ImageConversion.Core;

public sealed record SpaceEngineersBlockDefinition(
    string Id,
    string DisplayName,
    string GridSize,
    string TypeId,
    string SubtypeId,
    IReadOnlyList<SpaceEngineersComponentRequirement> Components,
    string? DlcName = null)
{
    public string DisplayLabel => $"{DisplayName} ({GridSize} Grid)";

    public string SearchText => $"{DisplayName} {GridSize} {TypeId} {SubtypeId} {DlcName}";
}
