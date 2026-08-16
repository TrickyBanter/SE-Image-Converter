namespace ImageConversion.Core;

public sealed record ResourceCalculationRequest(
    IReadOnlyList<SpaceEngineersBlockQuantity> Blocks,
    IReadOnlyList<ResourceRecipeQuantity> Recipes,
    IReadOnlyList<ResourceRecipe> RecipeDefinitions)
{
    public ResourceCalculationRequest(IReadOnlyList<SpaceEngineersBlockQuantity> blocks)
        : this(blocks, [], [])
    {
    }
}
