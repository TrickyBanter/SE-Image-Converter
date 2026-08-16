using ImageConversion.Core;
using Xunit;

namespace ImageConversion.Core.Tests;

public sealed class ResourceCalculatorTests
{
    private static readonly SpaceEngineersBlockDefinition SmallConveyor = new(
        "Conveyor/Small/Small",
        "Small Conveyor",
        "Small",
        "Conveyor",
        "Small",
        [
            new("Interior Plate", 2),
            new("Construction Component", 1),
        ]);

    private static readonly SpaceEngineersBlockDefinition LargeConveyor = new(
        "Conveyor/Large/Large",
        "Large Conveyor",
        "Large",
        "Conveyor",
        "Large",
        [
            new("Interior Plate", 10),
            new("Motor", 2),
        ]);

    private readonly ResourceCalculator calculator = new([SmallConveyor, LargeConveyor]);

    private static readonly ResourceRecipe MissileRecipe = new(
        "missile",
        "Missile",
        [
            new(SmallConveyor.Id, 20),
            new(LargeConveyor.Id, 1),
        ]);

    [Fact]
    public void MultipliesOneBlockRecipeByQuantity()
    {
        ResourceCalculationResult result = calculator.Calculate(new ResourceCalculationRequest(
            [new SpaceEngineersBlockQuantity(SmallConveyor.Id, 3)]));

        Assert.Contains(result.ComponentTotals, total => total is { ComponentName: "Interior Plate", Count: 6 });
        Assert.Contains(result.ComponentTotals, total => total is { ComponentName: "Construction Component", Count: 3 });
    }

    [Fact]
    public void SumsSharedComponentsAcrossBlocks()
    {
        ResourceCalculationResult result = calculator.Calculate(new ResourceCalculationRequest(
            [
                new SpaceEngineersBlockQuantity(SmallConveyor.Id, 2),
                new SpaceEngineersBlockQuantity(LargeConveyor.Id, 1),
            ]));

        Assert.Contains(result.ComponentTotals, total => total is { ComponentName: "Interior Plate", Count: 14 });
    }

    [Fact]
    public void KeepsSmallAndLargeGridVariantsDistinct()
    {
        ResourceCalculationResult result = calculator.Calculate(new ResourceCalculationRequest(
            [new SpaceEngineersBlockQuantity(LargeConveyor.Id, 1)]));

        Assert.DoesNotContain(result.ComponentTotals, total => total is { ComponentName: "Construction Component" });
        Assert.Contains(result.ComponentTotals, total => total is { ComponentName: "Motor", Count: 2 });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectsZeroAndNegativeQuantities(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(new ResourceCalculationRequest(
            [new SpaceEngineersBlockQuantity(SmallConveyor.Id, quantity)])));
    }

    [Fact]
    public void ReturnsEmptyTotalsForEmptySelection()
    {
        ResourceCalculationResult result = calculator.Calculate(new ResourceCalculationRequest([]));

        Assert.Empty(result.ComponentTotals);
    }

    [Fact]
    public void TotalsOneRecipe()
    {
        ResourceCalculationResult result = calculator.Calculate(new ResourceCalculationRequest(
            [],
            [new ResourceRecipeQuantity(MissileRecipe.Id, 1)],
            [MissileRecipe]));

        Assert.Contains(result.ComponentTotals, total => total is { ComponentName: "Interior Plate", Count: 50 });
        Assert.Contains(result.ComponentTotals, total => total is { ComponentName: "Construction Component", Count: 20 });
        Assert.Contains(result.ComponentTotals, total => total is { ComponentName: "Motor", Count: 2 });
    }

    [Fact]
    public void RecipeMultiplierScalesNestedBlocks()
    {
        ResourceCalculationResult result = calculator.Calculate(new ResourceCalculationRequest(
            [],
            [new ResourceRecipeQuantity(MissileRecipe.Id, 5)],
            [MissileRecipe]));

        Assert.Contains(result.ComponentTotals, total => total is { ComponentName: "Interior Plate", Count: 250 });
        Assert.Contains(result.ComponentTotals, total => total is { ComponentName: "Construction Component", Count: 100 });
        Assert.Contains(result.ComponentTotals, total => total is { ComponentName: "Motor", Count: 10 });
    }

    [Fact]
    public void DirectBlocksAndRecipeRowsSumTogether()
    {
        ResourceCalculationResult result = calculator.Calculate(new ResourceCalculationRequest(
            [new SpaceEngineersBlockQuantity(SmallConveyor.Id, 2)],
            [new ResourceRecipeQuantity(MissileRecipe.Id, 1)],
            [MissileRecipe]));

        Assert.Contains(result.ComponentTotals, total => total is { ComponentName: "Interior Plate", Count: 54 });
        Assert.Contains(result.ComponentTotals, total => total is { ComponentName: "Construction Component", Count: 22 });
    }

    [Fact]
    public void MultipleRecipeRowsSharingComponentsAggregate()
    {
        ResourceRecipe tinyRecipe = new(
            "tiny",
            "Tiny",
            [new SpaceEngineersBlockQuantity(SmallConveyor.Id, 1)]);

        ResourceCalculationResult result = calculator.Calculate(new ResourceCalculationRequest(
            [],
            [
                new ResourceRecipeQuantity(MissileRecipe.Id, 1),
                new ResourceRecipeQuantity(tinyRecipe.Id, 3),
            ],
            [MissileRecipe, tinyRecipe]));

        Assert.Contains(result.ComponentTotals, total => total is { ComponentName: "Interior Plate", Count: 56 });
        Assert.Contains(result.ComponentTotals, total => total is { ComponentName: "Construction Component", Count: 23 });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectsZeroAndNegativeRecipeQuantities(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(new ResourceCalculationRequest(
            [],
            [new ResourceRecipeQuantity(MissileRecipe.Id, quantity)],
            [MissileRecipe])));
    }

    [Fact]
    public void RejectsUnknownRecipeIds()
    {
        Assert.Throws<KeyNotFoundException>(() => calculator.Calculate(new ResourceCalculationRequest(
            [],
            [new ResourceRecipeQuantity("missing", 1)],
            [MissileRecipe])));
    }

    [Fact]
    public void RejectsUnknownBlockIdsInsideRecipes()
    {
        ResourceRecipe recipe = new("bad", "Bad", [new SpaceEngineersBlockQuantity("missing", 1)]);

        Assert.Throws<KeyNotFoundException>(() => calculator.Calculate(new ResourceCalculationRequest(
            [],
            [new ResourceRecipeQuantity(recipe.Id, 1)],
            [recipe])));
    }
}
