using System.Reflection;
using System.Text.Json;

namespace ImageConversion.Core;

public static class SpaceEngineersBlockCatalog
{
    private const string DefaultCatalogResourceName = "ImageConversion.Core.Data.SpaceEngineersVanillaBlocks.json";

    // Block IDs used by earlier catalog versions, mapped to their current game IDs so saved recipes keep working.
    private static readonly IReadOnlyDictionary<string, string> LegacyBlockIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["MyObjectBuilder_Projector/LargeProjector/Large"] = "Projector/LargeProjector/Large",
        ["MyObjectBuilder_Projector/SmallProjector/Small"] = "Projector/SmallProjector/Small",
        ["Cockpit/PassengerSeatSmall/Small"] = "Cockpit/PassengerSeatSmallNew/Small",
        ["EventControllerBlock/LargeBlockEventControllerBlock/Large"] = "EventControllerBlock/EventControllerLarge/Large",
        ["EventControllerBlock/SmallBlockEventControllerBlock/Small"] = "EventControllerBlock/EventControllerSmall/Small",
        ["PathRecorderBlock/LargeBlockPathRecorderBlock/Large"] = "PathRecorderBlock/LargePathRecorderBlock/Large",
        ["PathRecorderBlock/SmallBlockPathRecorderBlock/Small"] = "PathRecorderBlock/SmallPathRecorderBlock/Small",
        ["BasicMissionBlock/LargeBlockBasicMissionBlock/Large"] = "BasicMissionBlock/LargeBasicMission/Large",
        ["BasicMissionBlock/SmallBlockBasicMissionBlock/Small"] = "BasicMissionBlock/SmallBasicMission/Small",
        ["FlightMovementBlock/LargeBlockFlightMovementBlock/Large"] = "FlightMovementBlock/LargeFlightMovement/Large",
        ["FlightMovementBlock/SmallBlockFlightMovementBlock/Small"] = "FlightMovementBlock/SmallFlightMovement/Small",
        ["DefensiveCombatBlock/LargeBlockDefensiveCombatBlock/Large"] = "DefensiveCombatBlock/LargeDefensiveCombat/Large",
        ["DefensiveCombatBlock/SmallBlockDefensiveCombatBlock/Small"] = "DefensiveCombatBlock/SmallDefensiveCombat/Small",
        ["OffensiveCombatBlock/LargeBlockOffensiveCombatBlock/Large"] = "OffensiveCombatBlock/LargeOffensiveCombat/Large",
        ["OffensiveCombatBlock/SmallBlockOffensiveCombatBlock/Small"] = "OffensiveCombatBlock/SmallOffensiveCombat/Small",
        ["Wheel/OffroadRealWheel2x2Mirrored/Large"] = "Wheel/OffroadRealWheel2x2/Large",
        ["Wheel/OffroadSmallRealWheel2x2Mirrored/Small"] = "Wheel/OffroadSmallRealWheel2x2/Small",
    };

    public static IReadOnlyList<SpaceEngineersBlockDefinition> DefaultBlocks { get; } = LoadDefault();

    /// <summary>Returns the current block ID for an ID saved by an older catalog version.</summary>
    public static string ResolveBlockId(string blockId) =>
        LegacyBlockIds.TryGetValue(blockId, out string? currentId) ? currentId : blockId;

    private static IReadOnlyList<SpaceEngineersBlockDefinition> LoadDefault()
    {
        Assembly assembly = typeof(SpaceEngineersBlockCatalog).Assembly;
        using Stream? stream = assembly.GetManifestResourceStream(DefaultCatalogResourceName);

        if (stream is null)
        {
            throw new InvalidOperationException($"Could not load embedded resource '{DefaultCatalogResourceName}'.");
        }

        List<SpaceEngineersBlockDefinition>? blocks = JsonSerializer.Deserialize<List<SpaceEngineersBlockDefinition>>(
            stream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        if (blocks is null)
        {
            throw new InvalidOperationException("The Space Engineers block catalog is empty or invalid.");
        }

        return blocks;
    }
}
