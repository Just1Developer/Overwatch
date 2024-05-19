using System.Collections.Concurrent;
using CSharp_Kubernetes.Proxy;

namespace CSharp_Kubernetes.Overwatch;

public class Overwatch
{
    private static CancellationTokenSource? _cancellationToken;
    
    public static async Task Launch()
    {
        Console.WriteLine("Launching Overwatch...");
        _cancellationToken = new CancellationTokenSource();
        Executable.PrintInfo();
        
        ConcurrentBag<Task> tasks = new();
        tasks.Add(Servers.Servers.LaunchServers(httpsServers: 3, rootServer: false));
        tasks.Add(ProxyServerHandler.StartReverseProxies());
        tasks.Add(BeginHandleInput());
        
        Console.WriteLine("Overwatch is live.");

        await Task.WhenAll(tasks);
    }

    private static async Task BeginHandleInput()
    {
        while (_cancellationToken != null && !_cancellationToken.IsCancellationRequested)
        {
            string line = (await Console.In.ReadLineAsync() ?? "").Trim();
            if (!string.IsNullOrEmpty(line))
            {
                if (line == "ow update")
                {
                    Console.WriteLine("Overwatch Updates are a WIP");
                }
                if (line == "update")
                {
                    Console.WriteLine("Updates are a WIP");
                }
                if (line == "on")
                {
                    Console.WriteLine("Yes");
                }
                if (line == "list")
                {
                    Console.WriteLine("Will be implementing that soon");
                }
                else
                {
                    Console.WriteLine("\n/////////////////////////////////////////////////////" +
                                      "\n\nOverwatch | Help:" +
                                      "\n\now update: Updates Overwatch" +
                                      "\nupdate: Updates and restarts the server" +
                                      "\non: If Overwatch is on" +
                                      "\nlist: Lists all running servers and proxies" +
                                      "\n\n/////////////////////////////////////////////////////\n");
                }
            }
        }
    }
}