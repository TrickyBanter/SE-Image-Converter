using ImageConversion.Core;
using Xunit;

namespace ImageConversion.Core.Tests;

public sealed class SpaceEngineersGpsParserTests
{
    [Fact]
    public void ParsesValidSpaceEngineersGpsString()
    {
        bool parsed = SpaceEngineersGpsParser.TryParse(
            "GPS:Home Base:123.5:-456:7890.25:#FF75C9F1:",
            out JumpDriveVector vector);

        Assert.True(parsed);
        Assert.Equal(123.5, vector.X);
        Assert.Equal(-456, vector.Y);
        Assert.Equal(7890.25, vector.Z);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Home Base:123:456:789")]
    [InlineData("GPS:Home Base:abc:456:789:#FF75C9F1:")]
    [InlineData("GPS:Home Base:123:456:#FF75C9F1:")]
    public void RejectsInvalidGpsString(string value)
    {
        bool parsed = SpaceEngineersGpsParser.TryParse(value, out _);

        Assert.False(parsed);
    }
}
