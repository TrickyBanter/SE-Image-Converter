namespace ImageConversion.Core;

public sealed record JumpDriveLeg(
    int Number,
    double DistanceKm,
    TimeSpan RechargeWaitBeforeNextJump);
