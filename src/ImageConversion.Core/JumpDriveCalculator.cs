namespace ImageConversion.Core;

public sealed class JumpDriveCalculator
{
    public static readonly TimeSpan JumpCountdown = TimeSpan.FromSeconds(10);
    public const double MinimumJumpDistanceKm = 5;

    public JumpDriveCalculationResult Calculate(JumpDriveCalculationRequest request)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(request.JumpDriveCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(request.ShipMassKg, 0);

        double totalDistanceKm = request.Start.DistanceTo(request.Destination) / 1000;

        if (totalDistanceKm < MinimumJumpDistanceKm)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                $"Destination must be at least {MinimumJumpDistanceKm:N0} km away.");
        }

        JumpDriveProfile profile = JumpDriveProfile.FromType(request.DriveType);
        double effectiveMaxRangeKm = CalculateEffectiveMaxRangeKm(request.JumpDriveCount, request.ShipMassKg, profile.Type);
        int jumpCount = (int)Math.Ceiling(totalDistanceKm / effectiveMaxRangeKm);
        List<JumpDriveLeg> legs = new(jumpCount);
        TimeSpan totalTravelTime = TimeSpan.Zero;
        double remainingDistanceKm = totalDistanceKm;

        for (int index = 0; index < jumpCount; index++)
        {
            bool isFinalLeg = index == jumpCount - 1;
            double legDistanceKm = Math.Min(effectiveMaxRangeKm, remainingDistanceKm);
            TimeSpan rechargeWait = isFinalLeg
                ? TimeSpan.Zero
                : ScaleRechargeWait(legDistanceKm, effectiveMaxRangeKm, profile);

            legs.Add(new JumpDriveLeg(index + 1, legDistanceKm, rechargeWait));
            totalTravelTime += JumpCountdown + rechargeWait;
            remainingDistanceKm -= legDistanceKm;
        }

        return new JumpDriveCalculationResult(
            totalDistanceKm,
            effectiveMaxRangeKm,
            jumpCount,
            totalTravelTime,
            legs);
    }

    public static double CalculateEffectiveMaxRangeKm(
        int jumpDriveCount,
        double shipMassKg,
        JumpDriveType driveType = JumpDriveType.Standard)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(jumpDriveCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(shipMassKg, 0);

        JumpDriveProfile profile = JumpDriveProfile.FromType(driveType);
        double baseRangeKm = profile.BaseRangeKmPerDrive * jumpDriveCount;

        return shipMassKg <= profile.FullRangeMassThresholdKg
            ? baseRangeKm
            : baseRangeKm * profile.FullRangeMassThresholdKg / shipMassKg;
    }

    private static TimeSpan ScaleRechargeWait(
        double legDistanceKm,
        double effectiveMaxRangeKm,
        JumpDriveProfile profile)
    {
        double chargeRatio = legDistanceKm / effectiveMaxRangeKm;
        return TimeSpan.FromSeconds(profile.FullRechargeTime.TotalSeconds * chargeRatio);
    }
}
