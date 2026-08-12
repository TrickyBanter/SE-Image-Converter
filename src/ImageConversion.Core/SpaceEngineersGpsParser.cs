using System.Globalization;

namespace ImageConversion.Core;

public static class SpaceEngineersGpsParser
{
    public static bool TryParse(string value, out JumpDriveVector vector)
    {
        vector = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string[] parts = value.Trim().Split(':');

        if (parts.Length < 6 || !parts[0].Equals("GPS", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) ||
            !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double y) ||
            !double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
        {
            return false;
        }

        vector = new JumpDriveVector(x, y, z);
        return true;
    }
}
