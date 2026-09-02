using ImageConversion.Core;
using Xunit;

namespace ImageConversion.Core.Tests;

public sealed class SpaceEngineersBlockCatalogTests
{
    [Fact]
    public void CatalogLoadsSuccessfully()
    {
        Assert.NotEmpty(SpaceEngineersBlockCatalog.DefaultBlocks);
    }

    [Fact]
    public void EveryBlockHasStableIdentityAndDisplayName()
    {
        Assert.All(SpaceEngineersBlockCatalog.DefaultBlocks, block =>
        {
            Assert.False(string.IsNullOrWhiteSpace(block.Id));
            Assert.False(string.IsNullOrWhiteSpace(block.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(block.GridSize));
            Assert.False(string.IsNullOrWhiteSpace(block.TypeId));
            Assert.NotNull(block.SubtypeId);
        });
    }

    [Fact]
    public void EveryComponentQuantityIsPositive()
    {
        Assert.All(SpaceEngineersBlockCatalog.DefaultBlocks.SelectMany(block => block.Components), component =>
        {
            Assert.False(string.IsNullOrWhiteSpace(component.ComponentName));
            Assert.True(component.Count > 0);
        });
    }

    [Fact]
    public void CatalogDoesNotContainDuplicateBlockIds()
    {
        IReadOnlyList<string> ids = SpaceEngineersBlockCatalog.DefaultBlocks.Select(block => block.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void CatalogIncludesSmallGridSmallBatteryVariant()
    {
        SpaceEngineersBlockDefinition block = Assert.Single(
            SpaceEngineersBlockCatalog.DefaultBlocks,
            block => block.Id.Equals("BatteryBlock/SmallBlockSmallBatteryBlock/Small", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("Small Battery", block.DisplayName);
        Assert.Contains(block.Components, component => component is { ComponentName: "Power Cell", Count: 2 });
    }

    [Theory]
    [InlineData("AI Flight (Move)")]
    [InlineData("AI Basic (Task)")]
    [InlineData("AI Recorder (Task)")]
    [InlineData("AI Defensive (Combat)")]
    [InlineData("AI Offensive (Combat)")]
    [InlineData("Event Controller")]
    public void CatalogIncludesAutomatonsBlocksForBothGridSizes(string displayName)
    {
        Assert.Contains(SpaceEngineersBlockCatalog.DefaultBlocks, block =>
            block.DisplayName == displayName && block.GridSize == "Large");
        Assert.Contains(SpaceEngineersBlockCatalog.DefaultBlocks, block =>
            block.DisplayName == displayName && block.GridSize == "Small");
    }

    [Fact]
    public void CatalogIncludesExpectedAiBlockRecipe()
    {
        SpaceEngineersBlockDefinition block = Assert.Single(
            SpaceEngineersBlockCatalog.DefaultBlocks,
            block => block.Id.Equals("FlightMovementBlock/SmallBlockFlightMovementBlock/Small", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(block.Components, component => component is { ComponentName: "Detector Components", Count: 4 });
        Assert.Contains(block.Components, component => component is { ComponentName: "Computer", Count: 10 });
        Assert.Contains(block.Components, component => component is { ComponentName: "Steel Plate", Count: 2 });
    }

    [Fact]
    public void CatalogIncludesExpectedEventControllerRecipe()
    {
        SpaceEngineersBlockDefinition block = Assert.Single(
            SpaceEngineersBlockCatalog.DefaultBlocks,
            block => block.Id.Equals("EventControllerBlock/LargeBlockEventControllerBlock/Large", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(block.Components, component => component is { ComponentName: "Interior Plate", Count: 10 });
        Assert.Contains(block.Components, component => component is { ComponentName: "Display", Count: 4 });
        Assert.Contains(block.Components, component => component is { ComponentName: "Construction Component", Count: 30 });
    }
}
