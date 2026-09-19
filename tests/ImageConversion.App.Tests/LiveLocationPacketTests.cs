using System.Text.Json;
using ImageConversion.App.Services;
using Xunit;

namespace ImageConversion.App.Tests;

public sealed class LiveLocationPacketTests
{
    [Fact]
    public void DeserializesTheLowercasePluginProtocol()
    {
        LiveLocationPacket? packet = JsonSerializer.Deserialize<LiveLocationPacket>(
            "{\"protocol\":\"setoolkit-live-location/v1\",\"x\":123.5,\"y\":-456,\"z\":789}",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(packet);
        Assert.Equal(LiveLocationPacket.ProtocolName, packet.Protocol);
        Assert.Equal(123.5, packet.X);
        Assert.Equal(-456, packet.Y);
        Assert.Equal(789, packet.Z);
    }
}
