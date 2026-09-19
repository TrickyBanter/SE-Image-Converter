using ImageConversion.Core;
using Xunit;

namespace ImageConversion.Core.Tests;

public sealed class StarSystemMapTests
{
    [Fact]
    public void VanillaBodiesContainEveryStandardStarSystemBody()
    {
        Assert.Equal(
            ["Earthlike", "Moon", "Mars", "Europa", "Alien", "Titan", "Triton", "Pertam"],
            StarSystemMap.VanillaBodies.Select(body => body.Name));
    }

    [Fact]
    public void EarthlikeIsAtGpsOrigin()
    {
        StarSystemBody earthlike = Assert.Single(StarSystemMap.VanillaBodies, body => body.Name == "Earthlike");

        Assert.Equal(new JumpDriveVector(0, 0, 0), earthlike.Position);
    }
}
