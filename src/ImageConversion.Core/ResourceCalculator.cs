namespace ImageConversion.Core;

public sealed class ResourceCalculator
{
    private readonly IReadOnlyDictionary<string, SpaceEngineersBlockDefinition> blocksById;

    public ResourceCalculator(IEnumerable<SpaceEngineersBlockDefinition> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        blocksById = blocks.ToDictionary(block => block.Id, StringComparer.OrdinalIgnoreCase);
    }

    public ResourceCalculationResult Calculate(ResourceCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        SortedDictionary<string, int> totals = new(StringComparer.OrdinalIgnoreCase);

        foreach (SpaceEngineersBlockQuantity item in request.Blocks)
        {
            if (item.Quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Block quantities must be greater than 0.");
            }

            if (!blocksById.TryGetValue(item.BlockId, out SpaceEngineersBlockDefinition? block))
            {
                throw new KeyNotFoundException($"Block '{item.BlockId}' is not in the resource catalog.");
            }

            foreach (SpaceEngineersComponentRequirement component in block.Components)
            {
                totals.TryGetValue(component.ComponentName, out int existingCount);
                totals[component.ComponentName] = checked(existingCount + (component.Count * item.Quantity));
            }
        }

        return new ResourceCalculationResult(
            totals.Select(total => new ResourceComponentTotal(total.Key, total.Value)).ToList());
    }
}
