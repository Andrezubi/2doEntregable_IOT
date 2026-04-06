using System.Net;
using System.Net.Sockets;
using System.Text;
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

        Console.WriteLine("Server started...");

        while (true)
        {
            var client = await server.AcceptTcpClientAsync();
            _ = HandleClient(client);
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        Console.WriteLine("Client connected");

        var stream = client.GetStream();


        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };


        byte[] buffer = new byte[1024];
        string dataBuffer = "";

        string? deviceId = null;

        try
        {
            while (true)
            {

                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    Console.WriteLine("Client disconnected");
                    break;
                }


                dataBuffer += Encoding.UTF8.GetString(buffer, 0, bytesRead);

                int newlineIndex;


                while ((newlineIndex = dataBuffer.IndexOf('\n')) != -1)
                {
                    string msg = dataBuffer.Substring(0, newlineIndex).Trim();
                    dataBuffer = dataBuffer.Substring(newlineIndex + 1);

                    if (string.IsNullOrEmpty(msg)) continue;

                    Console.WriteLine($"RX: {msg}");

                    // =========================
                    // MESSAGE HANDLING
                    // =========================

                    if (msg.StartsWith("REGISTER:"))
                    {

                        var parts = msg.Split(':');
                        if (parts.Length >= 2)
                        {
                            deviceId = parts[1];

                            deviceManager.Register(deviceId, client);

                            await writer.WriteLineAsync("REGISTER:OK");

                            Console.WriteLine($"Registro {deviceId}");

                            if (deviceId == "ACTUATOR")
                            {
                                await writer.WriteLineAsync(zoneService.GetCommand());
                            }
                        }
                    }

                    else if (msg.StartsWith("DISTANCE:"))
                    {
                        var parts = msg.Split(':');

                        if (parts.Length >= 2)
                        {
                            var input = parts[1];

                            if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float dist))
                            {
                                Console.WriteLine($"Distance: {dist}");

                                if (zoneService.Update(dist))
                                {
                                    Console.WriteLine($"newConfig {zoneService.GetCommand()}");
                                    if (deviceManager.TryGet("ACTUATOR", out var act))
                                    {

                                        if (deviceManager.TryGet("ACTUATOR", out _, out var actWriter))
                                        {
                                            await actWriter.WriteLineAsync(zoneService.GetCommand());
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Parse error DISTANCE");
                            }
                        }
                    }

                    else if (msg.StartsWith("PING:"))
                    {
                        var parts = msg.Split(':');

                        if (parts.Length >= 2)
                        {
                            var pingId = parts[1];

                            deviceManager.UpdatePing(pingId);

                            await writer.WriteLineAsync("PING:OK");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client error: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine("Client closed");
        }
    }
}