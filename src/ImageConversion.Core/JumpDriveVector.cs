namespace ImageConversion.Core;

public readonly record struct JumpDriveVector(double X, double Y, double Z)
{
    public double DistanceTo(JumpDriveVector other)
    {
        double deltaX = other.X - X;
        double deltaY = other.Y - Y;
        double deltaZ = other.Z - Z;

        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ));
    }
}
