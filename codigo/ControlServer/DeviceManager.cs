using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
class DeviceManager
{
    private ConcurrentDictionary<string, TcpClient> clients = new();
    private ConcurrentDictionary<string, DateTime> lastSeen = new();

    public void Register(string id, TcpClient client)
    {
        clients[id] = client;
        lastSeen[id] = DateTime.Now;
    }

    public void UpdatePing(string id)
    {
        lastSeen[id] = DateTime.Now;
    }

    public bool TryGet(string id, out TcpClient client)
    {
        return clients.TryGetValue(id, out client);
    }

    public void Remove(string id)
    {
        if (clients.TryRemove(id, out var c))
        {
            c.Close();
        }
        lastSeen.TryRemove(id, out _);
    }

    public void Cleanup(int timeoutSeconds)
    {
        foreach (var device in lastSeen.Keys.ToList())
        {
            if ((DateTime.Now - lastSeen[device]).TotalSeconds > timeoutSeconds)
            {
                Console.WriteLine($" {device} timeout");
                Remove(device);
            }
        }
    }
}