using System.Collections.Concurrent;
using CSharp_Kubernetes.Proxy;
using CSharp_Kubernetes.Servers;

namespace CSharp_Kubernetes.Overwatch;

public class Overwatch
{
    public static async Task Launch()
    {
        Console.WriteLine("Launching Overwatch...");
        Servers.Servers.PrintInfo();
        
        ConcurrentBag<Task> tasks = new();
        tasks.Add(Servers.Servers.LaunchServers(rootServer: false));
        tasks.Add(ProxyServerInit.StartReverseProxies());
        
        Console.WriteLine("Overwatch is live.");

        await Task.WhenAll(tasks);
    }
}