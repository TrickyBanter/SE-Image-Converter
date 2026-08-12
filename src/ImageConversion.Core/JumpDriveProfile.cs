namespace ImageConversion.Core;

public sealed record JumpDriveProfile(
    JumpDriveType Type,
    string Name,
    double BaseRangeKmPerDrive,
    double FullRangeMassThresholdKg,
    TimeSpan FullRechargeTime)
{
    public static JumpDriveProfile Standard { get; } = new(
        JumpDriveType.Standard,
        "Standard Jump Drive",
        2000,
        1_250_000,
        TimeSpan.FromMinutes(7));

    public static JumpDriveProfile Prototech { get; } = new(
        JumpDriveType.Prototech,
        "Prototech Jump Drive",
        6000,
        2_500_000,
        TimeSpan.FromMinutes(5));

    public static JumpDriveProfile FromType(JumpDriveType type) => type switch
    {
        JumpDriveType.Prototech => Prototech,
        _ => Standard,
    };
}
