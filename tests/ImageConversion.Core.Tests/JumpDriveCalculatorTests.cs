using ImageConversion.Core;
using Xunit;

namespace ImageConversion.Core.Tests;

public sealed class JumpDriveCalculatorTests
{
    private readonly JumpDriveCalculator calculator = new();

    [Theory]
    [InlineData(1, 200_000, 2000)]
    [InlineData(2, 1_250_000, 4000)]
    public void CalculatesFullRangeAtOrBelowMassThreshold(int drives, double massKg, double expectedRangeKm)
    {
        double rangeKm = JumpDriveCalculator.CalculateEffectiveMaxRangeKm(drives, massKg);

        Assert.Equal(expectedRangeKm, rangeKm);
    }

    [Fact]
    public void ReducesRangeProportionallyAboveMassThreshold()
    {
        double rangeKm = JumpDriveCalculator.CalculateEffectiveMaxRangeKm(1, 2_500_000);

        Assert.Equal(1000, rangeKm);
    }

    [Fact]
    public void CalculatesPrototechFullRangeAtOrBelowMassThreshold()
    {
        double rangeKm = JumpDriveCalculator.CalculateEffectiveMaxRangeKm(1, 2_500_000, JumpDriveType.Prototech);

        Assert.Equal(6000, rangeKm);
    }

    [Fact]
    public void ReducesPrototechRangeProportionallyAboveMassThreshold()
    {
        double rangeKm = JumpDriveCalculator.CalculateEffectiveMaxRangeKm(1, 5_000_000, JumpDriveType.Prototech);

        Assert.Equal(3000, rangeKm);
    }

    [Fact]
    public void CalculatesStraightLineDistanceFromCoordinates()
    {
        JumpDriveCalculationResult result = calculator.Calculate(new JumpDriveCalculationRequest(
            new JumpDriveVector(0, 0, 0),
            new JumpDriveVector(3_000_000, 4_000_000, 0),
            3,
            1_250_000));

        Assert.Equal(5000, result.TotalDistanceKm);
    }

    [Fact]
    public void SplitsPartialFinalLegAndSkipsFinalRecharge()
    {
        JumpDriveCalculationResult result = calculator.Calculate(new JumpDriveCalculationRequest(
            new JumpDriveVector(0, 0, 0),
            new JumpDriveVector(4_500_000, 0, 0),
            1,
            1_250_000));

        Assert.Equal(3, result.JumpCount);
        Assert.Equal([2000, 2000, 500], result.Legs.Select(leg => leg.DistanceKm));
        Assert.Equal(TimeSpan.Zero, result.Legs[^1].RechargeWaitBeforeNextJump);
        Assert.Equal(TimeSpan.FromSeconds(870), result.TotalTravelTime);
    }

    [Fact]
    public void ExactRangeTripDoesNotAddExtraJump()
    {
        JumpDriveCalculationResult result = calculator.Calculate(new JumpDriveCalculationRequest(
            new JumpDriveVector(0, 0, 0),
            new JumpDriveVector(4_000_000, 0, 0),
            1,
            1_250_000));

        Assert.Equal(2, result.JumpCount);
        Assert.Equal([2000, 2000], result.Legs.Select(leg => leg.DistanceKm));
        Assert.Equal(TimeSpan.FromSeconds(440), result.TotalTravelTime);
    }

    [Fact]
    public void UsesPrototechRechargeTime()
    {
        JumpDriveCalculationResult result = calculator.Calculate(new JumpDriveCalculationRequest(
            new JumpDriveVector(0, 0, 0),
            new JumpDriveVector(12_000_000, 0, 0),
            1,
            2_500_000,
            JumpDriveType.Prototech));

        Assert.Equal(2, result.JumpCount);
        Assert.Equal([6000, 6000], result.Legs.Select(leg => leg.DistanceKm));
        Assert.Equal(TimeSpan.FromSeconds(320), result.TotalTravelTime);
    }

    [Fact]
    public void RejectsDestinationUnderMinimumJumpDistance()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(new JumpDriveCalculationRequest(
            new JumpDriveVector(0, 0, 0),
            new JumpDriveVector(4_999, 0, 0),
            1,
            1_250_000)));
    }
}
