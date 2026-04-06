using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Text;

class DeviceManager
{
    private record DeviceEntry(TcpClient Client, StreamWriter Writer);

    private ConcurrentDictionary<string, DeviceEntry> devices = new();
    private ConcurrentDictionary<string, DateTime> lastSeen = new();

    public void Register(string id, TcpClient client)
    {
        // Si ya existía, cerrar el anterior
        if (devices.TryRemove(id, out var old))
        {
            try { old.Client.Close(); } catch { }
        }

        var writer = new StreamWriter(client.GetStream(), Encoding.UTF8) { AutoFlush = true };
        devices[id] = new DeviceEntry(client, writer);
        lastSeen[id] = DateTime.Now;
    }

    public void UpdatePing(string id)
    {
        if (lastSeen.ContainsKey(id))
            lastSeen[id] = DateTime.Now;
    }

    public bool TryGet(string id, out TcpClient client)
    {
        if (devices.TryGetValue(id, out var entry))
        {
            client = entry.Client;
            return true;
        }
        client = null!;
        return false;
    }

    // ✅ Nuevo overload que devuelve también el Writer
    public bool TryGet(string id, out TcpClient client, out StreamWriter writer)
    {
        if (devices.TryGetValue(id, out var entry))
        {
            client = entry.Client;
            writer = entry.Writer;
            return true;
        }
        client = null!;
        writer = null!;
        return false;
    }

    public void Remove(string id)
    {
        if (devices.TryRemove(id, out var entry))
        {
            try { entry.Writer.Close(); } catch { }
            try { entry.Client.Close(); } catch { }
        }
        lastSeen.TryRemove(id, out _);
    }

    public void Cleanup(int timeoutSeconds)
    {
        foreach (var id in lastSeen.Keys.ToList())
        {
            if ((DateTime.Now - lastSeen[id]).TotalSeconds > timeoutSeconds)
            {
                Console.WriteLine($"{id} timeout — cerrando conexión");
                Remove(id);
            }
        }
    }
}