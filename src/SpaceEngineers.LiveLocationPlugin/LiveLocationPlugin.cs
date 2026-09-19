using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Plugins;

namespace SEToolkit.LiveLocation;

/// <summary>
/// Read-only Space Engineers client plugin. It publishes the locally controlled
/// player's position to the SEToolkit desktop app over loopback UDP only.
/// </summary>
public sealed class LiveLocationPlugin : IPlugin
{
    private const int Port = 38023;
    private const int UpdateIntervalFrames = 12;
    private const string Protocol = "setoolkit-live-location/v1";

    private readonly UdpClient udpClient = new UdpClient();
    private readonly IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, Port);
    private readonly string diagnosticLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Pulsar",
        "Legacy",
        "SEToolkit.LiveLocation.log");
    private readonly string locationFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SEToolkit",
        "live-location.json");
    private int frameCount;
    private DateTime lastDiagnosticUtc;

    public void Init(object gameInstance)
    {
        WriteDiagnostic("Plugin initialized.");
    }

    public void Update()
    {
        if (++frameCount < UpdateIntervalFrames)
        {
            return;
        }

        frameCount = 0;

        try
        {
            // Plugins run inside the game client, so the session singleton is the
            // most reliable source. The public Mod API remains a fallback.
            var player = MySession.Static?.LocalHumanPlayer ?? MyAPIGateway.Session?.Player;
            if (player is null)
            {
                WriteDiagnostic("Waiting for a loaded local player session.");
                return;
            }

            var position = player.GetPosition();
            string json = "{\"protocol\":\"" + Protocol + "\",\"x\":" +
                          position.X.ToString("R", CultureInfo.InvariantCulture) + ",\"y\":" +
                          position.Y.ToString("R", CultureInfo.InvariantCulture) + ",\"z\":" +
                          position.Z.ToString("R", CultureInfo.InvariantCulture) + "}";
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            udpClient.Send(bytes, bytes.Length, endpoint);
            Directory.CreateDirectory(Path.GetDirectoryName(locationFilePath));
            File.WriteAllText(locationFilePath, json);
            WriteDiagnostic("Position packet sent to 127.0.0.1:38023.");
        }
        catch (Exception exception)
        {
            WriteDiagnostic($"Position update failed: {exception.GetType().Name}: {exception.Message}");
        }
    }

    public void Dispose()
    {
        udpClient.Dispose();
    }

    private void WriteDiagnostic(string message)
    {
        DateTime now = DateTime.UtcNow;
        if (now - lastDiagnosticUtc < TimeSpan.FromSeconds(5))
        {
            return;
        }

        lastDiagnosticUtc = now;

        try
        {
            File.AppendAllText(diagnosticLogPath, $"{now:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never interfere with Space Engineers.
        }
    }
}
