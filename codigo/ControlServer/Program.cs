using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

var deviceManager = new DeviceManager();
var zoneService = new ZoneService();
var tcpServer = new TcpServer(deviceManager, zoneService);
string hostName = Dns.GetHostName();
IPHostEntry ipEntry = Dns.GetHostEntry(hostName);
var localIp = ipEntry.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
Console.WriteLine($"Local IP: {localIp}");

_ = tcpServer.Start();

// loop limpieza
_ = Task.Run(async () =>
{
    while (true)
    {
        deviceManager.Cleanup(10);
        await Task.Delay(2000);
    }
});


// Endpoint para actualizar valores
app.MapGet("/setConfig", async (
    float gLow, float gHigh, bool gBlink,
    float yLow, float yHigh, bool yBlink,
    float rLow, float rHigh, bool rBlink,
    float bLow, float bHigh, bool bBlink
) =>
{

    if(bLow<2 || rLow<2 || yLow<2||gLow<2){
        return "No se puede tener una distancia menor a 2cm";

    }
    else if(bHigh>250||rHigh>250||yHigh>250||gHigh>250){
        return "No se puede tener una distancia mayor a 250cm";

    }
    else if(bLow>=bHigh||rLow>=rHigh||yLow>=yHigh||gLow>=gHigh){
        return "El limite inferior debe ser menor al limite superior";

    }
    zoneService.greenLower = gLow;
    zoneService.greenUpper = gHigh;
    zoneService.greenBlink = gBlink;

    zoneService.yellowLower = yLow;
    zoneService.yellowUpper = yHigh;
    zoneService.yellowBlink = yBlink;

    zoneService.redLower = rLow;
    zoneService.redUpper = rHigh;
    zoneService.redBlink = rBlink;

    zoneService.blueLower = bLow;
    zoneService.blueUpper = bHigh;
    zoneService.blueBlink = bBlink;

    
    if (zoneService.Update(zoneService.lastDistance)) 
    {
        if (deviceManager.TryGet("ACTUATOR", out var client))
        {
            try
            {
                var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
                await writer.WriteLineAsync(zoneService.GetCommand());
            }
            catch
            {
                deviceManager.Remove("ACTUATOR");
            }
        }
    }
    return "Configuración actualizada correctamente";
        
    
});

app.Run();
