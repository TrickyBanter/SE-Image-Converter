namespace ImageConversion.Core;

public sealed record JumpDriveCalculationRequest(
    JumpDriveVector Start,
    JumpDriveVector Destination,
    int JumpDriveCount,
    double ShipMassKg,
    JumpDriveType DriveType = JumpDriveType.Standard);
