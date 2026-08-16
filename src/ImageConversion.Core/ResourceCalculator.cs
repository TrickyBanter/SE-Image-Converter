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
        IReadOnlyDictionary<string, ResourceRecipe> recipesById = request.RecipeDefinitions
            .ToDictionary(recipe => recipe.Id, StringComparer.OrdinalIgnoreCase);

        foreach (SpaceEngineersBlockQuantity item in request.Blocks)
        {
            AddBlockTotals(totals, item.BlockId, item.Quantity);
        }

        foreach (ResourceRecipeQuantity item in request.Recipes)
        {
            if (item.Quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Recipe quantities must be greater than 0.");
            }

            if (!recipesById.TryGetValue(item.RecipeId, out ResourceRecipe? recipe))
            {
                throw new KeyNotFoundException($"Recipe '{item.RecipeId}' is not in the recipe library.");
            }

            foreach (SpaceEngineersBlockQuantity block in recipe.Blocks)
            {
                AddBlockTotals(totals, block.BlockId, checked(block.Quantity * item.Quantity));
            }
        }

        return new ResourceCalculationResult(
            totals.Select(total => new ResourceComponentTotal(total.Key, total.Value)).ToList());
    }

    private void AddBlockTotals(SortedDictionary<string, int> totals, string blockId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Block quantities must be greater than 0.");
        }

        if (!blocksById.TryGetValue(blockId, out SpaceEngineersBlockDefinition? block))
        {
            throw new KeyNotFoundException($"Block '{blockId}' is not in the resource catalog.");
        }

        foreach (SpaceEngineersComponentRequirement component in block.Components)
        {
            totals.TryGetValue(component.ComponentName, out int existingCount);
            totals[component.ComponentName] = checked(existingCount + (component.Count * quantity));
        }
    }
}
