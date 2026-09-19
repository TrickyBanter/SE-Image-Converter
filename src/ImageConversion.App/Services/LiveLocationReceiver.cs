using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace ImageConversion.App.Services;

public sealed class LiveLocationReceiver : IDisposable
{
    public const int Port = 38023;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly object syncRoot = new();
    private CancellationTokenSource? cancellation;
    private UdpClient? client;

    public event EventHandler<LiveLocationReceivedEventArgs>? LocationReceived;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        lock (syncRoot)
        {
            if (IsRunning)
            {
                return;
            }

            cancellation = new CancellationTokenSource();
            client = new UdpClient(new IPEndPoint(IPAddress.Loopback, Port));
            IsRunning = true;
            _ = ReceiveAsync(client, cancellation.Token);
        }
    }

    public void Stop()
    {
        lock (syncRoot)
        {
            cancellation?.Cancel();
            client?.Dispose();
            cancellation?.Dispose();
            cancellation = null;
            client = null;
            IsRunning = false;
        }
    }

    public void Dispose() => Stop();

    private async Task ReceiveAsync(UdpClient listener, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult result = await listener.ReceiveAsync(cancellationToken);
                LiveLocationPacket? packet = JsonSerializer.Deserialize<LiveLocationPacket>(result.Buffer, JsonOptions);

                if (packet is not null && packet.Protocol == LiveLocationPacket.ProtocolName &&
                    double.IsFinite(packet.X) && double.IsFinite(packet.Y) && double.IsFinite(packet.Z))
                {
                    LocationReceived?.Invoke(this, new LiveLocationReceivedEventArgs(packet.X, packet.Y, packet.Z));
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            lock (syncRoot)
            {
                if (ReferenceEquals(client, listener))
                {
                    IsRunning = false;
                }
            }
        }
    }
}

public sealed record LiveLocationPacket(string Protocol, double X, double Y, double Z)
{
    public const string ProtocolName = "setoolkit-live-location/v1";
}

public static class LiveLocationFileReader
{
    public static string LocationFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SEToolkit",
        "live-location.json");

    public static bool TryRead(out LiveLocationPacket? packet)
    {
        packet = null;

        try
        {
            if (!File.Exists(LocationFilePath))
            {
                return false;
            }

            packet = JsonSerializer.Deserialize<LiveLocationPacket>(File.ReadAllText(LocationFilePath), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            return packet is not null && packet.Protocol == LiveLocationPacket.ProtocolName &&
                   double.IsFinite(packet.X) && double.IsFinite(packet.Y) && double.IsFinite(packet.Z);
        }
        catch (IOException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed class LiveLocationReceivedEventArgs(double x, double y, double z) : EventArgs
{
    public double X { get; } = x;

    public double Y { get; } = y;

    public double Z { get; } = z;
}
