using System.Reflection;
using System.Text.Json;

namespace ImageConversion.Core;

public static class SpaceEngineersBlockCatalog
{
    private const string DefaultCatalogResourceName = "ImageConversion.Core.Data.SpaceEngineersVanillaBlocks.json";

    public static IReadOnlyList<SpaceEngineersBlockDefinition> DefaultBlocks { get; } = LoadDefault();

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
