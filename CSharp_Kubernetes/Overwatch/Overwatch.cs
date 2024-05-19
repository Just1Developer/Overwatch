using System.Collections.Concurrent;
using CSharp_Kubernetes.Proxy;

namespace CSharp_Kubernetes.Overwatch;

public class Overwatch
{
    public static async Task Launch()
    {
        Console.WriteLine("Launching Overwatch...");
        Servers.Servers.PrintInfo();
        
        ConcurrentBag<Task> tasks = new();
        tasks.Add(Servers.Servers.LaunchServers(httpsServers: 3, rootServer: false));
        tasks.Add(ProxyServerHandler.StartReverseProxies());
        
        Console.WriteLine("Overwatch is live.");

        await Task.WhenAll(tasks);
    }
}