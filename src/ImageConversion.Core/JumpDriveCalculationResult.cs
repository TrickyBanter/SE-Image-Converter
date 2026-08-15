namespace ImageConversion.Core;

public sealed record JumpDriveCalculationResult(
    double TotalDistanceKm,
    double EffectiveMaxRangeKm,
    int JumpCount,
    TimeSpan TotalTravelTime,
    IReadOnlyList<JumpDriveLeg> Legs);
