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
}
