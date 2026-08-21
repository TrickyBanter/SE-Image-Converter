using ImageConversion.App.Services;
using ImageConversion.Core;
using Xunit;

namespace ImageConversion.App.Tests;

public sealed class ResourceRecipeStorageServiceTests
{
    [Fact]
    public void RoundTripsRecipeJson()
    {
        string filePath = CreateTempFilePath();
        ResourceRecipeStorageService storage = new(filePath);
        ResourceRecipe recipe = Recipe("missile", "Missile");

        storage.Save([recipe]);
        ResourceRecipeStorageLoadResult result = storage.Load();

        ResourceRecipe loaded = Assert.Single(result.Recipes);
        Assert.Null(result.WarningMessage);
        Assert.Equal(recipe.Id, loaded.Id);
        Assert.Equal(recipe.Name, loaded.Name);
        Assert.Equal(recipe.Blocks, loaded.Blocks);
    }

    [Fact]
    public void MissingRecipeFileReturnsEmptyLibrary()
    {
        ResourceRecipeStorageService storage = new(CreateTempFilePath());

        ResourceRecipeStorageLoadResult result = storage.Load();

        Assert.Empty(result.Recipes);
        Assert.Null(result.WarningMessage);
    }

    [Fact]
    public void InvalidRecipeFileReturnsWarningAndEmptyLibrary()
    {
        string filePath = CreateTempFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, "not json");
        ResourceRecipeStorageService storage = new(filePath);

        ResourceRecipeStorageLoadResult result = storage.Load();

        Assert.Empty(result.Recipes);
        Assert.NotNull(result.WarningMessage);
    }

    [Fact]
    public void DuplicateRecipeNameReplacementPreservesOneRecipeEntry()
    {
        string filePath = CreateTempFilePath();
        ResourceRecipeStorageService storage = new(filePath);
        ResourceRecipe original = Recipe("one", "Missile");
        ResourceRecipe replacement = Recipe("two", "missile", 5);

        storage.Save([original]);
        IReadOnlyList<ResourceRecipe> updated = storage.UpsertRecipe([original], replacement);

        ResourceRecipe recipe = Assert.Single(updated);
        Assert.Equal(replacement.Id, recipe.Id);
        Assert.Equal(replacement.Name, recipe.Name);
        Assert.Equal(5, Assert.Single(recipe.Blocks).Quantity);
    }

    private static ResourceRecipe Recipe(string id, string name, int quantity = 2) => new(
        id,
        name,
        [new SpaceEngineersBlockQuantity("Conveyor/Small/Small", quantity)]);

    private static string CreateTempFilePath()
    {
        return Path.Combine(Path.GetTempPath(), "SEImageConverterTests", Guid.NewGuid().ToString("N"), "resource-recipes.json");
    }
}
