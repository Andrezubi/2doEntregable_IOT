using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Globalization;
class TcpServer
{
    private DeviceManager deviceManager;
    private ZoneService zoneService;

    public TcpServer(DeviceManager dm, ZoneService zs)
    {
        deviceManager = dm;
        zoneService = zs;
    }

    public async Task Start()
    {
        TcpListener server = new TcpListener(IPAddress.Any, 5000);
        server.Start();

        while (true)
        {
            var client = await server.AcceptTcpClientAsync();
            _ = HandleClient(client);
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        var reader = new StreamReader(client.GetStream());
        var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };

        string? deviceId = null;

        while (true)
        {
            var msg = await reader.ReadLineAsync();
            if (msg == null) break;

            if (msg.StartsWith("REGISTER:"))
            {
                deviceId = msg.Split(':')[1];
                deviceManager.Register(deviceId, client);
                await writer.WriteLineAsync("REGISTER:OK");
                Console.WriteLine($"Registro {deviceId}");
                if (deviceId == "ACTUATOR")
                {
                    await writer.WriteLineAsync(zoneService.GetCommand());
                }
            }

            if (msg.StartsWith("DISTANCE:"))
            {
                var input = msg.Split(":")[1];

                if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float dist))
                {
                    Console.WriteLine(dist);
                    if (zoneService.Update(dist))
                    {
                        if (deviceManager.TryGet("ACTUATOR", out var act))
                        {
                            var w = new StreamWriter(act.GetStream()) { AutoFlush = true };
                            await w.WriteLineAsync(zoneService.GetCommand());
                        }
                    }
                }
            }
            if (msg.StartsWith("PING:")){
                deviceId = msg.Split(":")[1];
                deviceManager.UpdatePing(deviceId);
                await writer.WriteLineAsync("PING:OK");

            }
        }
    }
}