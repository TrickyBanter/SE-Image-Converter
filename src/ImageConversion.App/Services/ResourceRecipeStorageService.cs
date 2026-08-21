using System.Text.Json;
using ImageConversion.Core;

namespace ImageConversion.App.Services;

public sealed class ResourceRecipeStorageService
{
    private const string RecipeFileName = "resource-recipes.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly string filePath;

    public ResourceRecipeStorageService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SE Toolkit",
            RecipeFileName))
    {
    }

    public ResourceRecipeStorageService(string filePath)
    {
        this.filePath = filePath;
    }

    public ResourceRecipeStorageLoadResult Load()
    {
        if (!File.Exists(filePath))
        {
            return new ResourceRecipeStorageLoadResult([], null);
        }

        try
        {
            string json = File.ReadAllText(filePath);
            List<ResourceRecipe>? recipes = JsonSerializer.Deserialize<List<ResourceRecipe>>(json, JsonOptions);

            return new ResourceRecipeStorageLoadResult(NormalizeRecipes(recipes ?? []), null);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new ResourceRecipeStorageLoadResult([], $"Could not load saved recipes: {ex.Message}");
        }
    }

    public void Save(IEnumerable<ResourceRecipe> recipes)
    {
        string? directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(NormalizeRecipes(recipes), JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public IReadOnlyList<ResourceRecipe> UpsertRecipe(IEnumerable<ResourceRecipe> recipes, ResourceRecipe recipe)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipe.Name);

        List<ResourceRecipe> updated = recipes
            .Where(existing => !existing.Name.Equals(recipe.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        updated.Add(recipe);
        IReadOnlyList<ResourceRecipe> normalized = NormalizeRecipes(updated);
        Save(normalized);
        return normalized;
    }

    private static IReadOnlyList<ResourceRecipe> NormalizeRecipes(IEnumerable<ResourceRecipe> recipes)
    {
        return recipes
            .Where(recipe => !string.IsNullOrWhiteSpace(recipe.Id) && !string.IsNullOrWhiteSpace(recipe.Name))
            .Select(recipe => recipe with
            {
                Name = recipe.Name.Trim(),
                Blocks = recipe.Blocks
                    .Where(block => !string.IsNullOrWhiteSpace(block.BlockId) && block.Quantity > 0)
                    .ToList(),
            })
            .OrderBy(recipe => recipe.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

public sealed record ResourceRecipeStorageLoadResult(
    IReadOnlyList<ResourceRecipe> Recipes,
    string? WarningMessage);
