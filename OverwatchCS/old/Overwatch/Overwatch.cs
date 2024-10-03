using System.Collections.Concurrent;
using System.Text;
using CSharp_Kubernetes.Deployment;
using CSharp_Kubernetes.Proxy;

namespace CSharp_Kubernetes.Overwatch;

public static class Overwatch
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
        Console.WriteLine("Overwatch is live.");
        tasks.Add(BeginHandleInput());
        

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
                    Console.WriteLine("Overwatch Updates are a WIP. Please update manually, for now.");
                }
                else if (line == "restart-all")
                {
                    Console.WriteLine("\n/////////////////////////////////////////////////////" +
                                      "\n\nRestarting All Servers..." +
                                      "\n\n/////////////////////////////////////////////////////\n");
                    await ServerUpdater.UpdateServers();
                    Console.WriteLine("\n/////////////////////////////////////////////////////" +
                                      $"\n\nRestart complete.\n" +
                                      $"\nSSL Servers: {ProxyServerHandler.SSLSecurePorts.Count}" +
                                      $"\nHTTP Servers: {ProxyServerHandler.InsecurePorts.Count}" +
                                      $"\nROOT Server: {(ProxyServerHandler.ROOT_SERVER_PORT > 0 ? "Yes" : "No")}" +
                                      "\n\n/////////////////////////////////////////////////////\n");
                }
                else if (line == "deploy")
                {
                    await WebDeployment.DeployNew();
                }
                else if (line == "on")
                {
                    Console.WriteLine("Yes");
                }
                else if (line == "list")
                {
                    StringBuilder ssl = new StringBuilder();
                    foreach (int port in ProxyServerHandler.SSLSecurePorts)
                    {
                        ssl.AppendLine($"  -> {port}");
                    }
                    if (ssl.Length == 0) ssl.AppendLine("Running SSL Host Proxies:\n  None");
                    else ssl.Insert(0, "Running SSL Host Proxies:\n");
                    
                    StringBuilder http = new StringBuilder();
                    foreach (int port in ProxyServerHandler.InsecurePorts)
                    {
                        http.AppendLine($"  -> {port}");
                    }
                    if (http.Length == 0) http.AppendLine("Running HTTP Host Proxies:\n  None");
                    else http.Insert(0, "Running HTTP Host Proxies:\n");

                    string host = ProxyServerHandler.ROOT_SERVER_PORT < 0 ? "ROOT: -" : $"ROOT: {ProxyServerHandler.ROOT_SERVER_PORT}";
                    
                    Console.WriteLine("\n/////////////////////////////////////////////////////" +
                                      "\n\nComplete Proxy List:" +
                                      $"\n{ssl}" +
                                      $"\n{http}" +
                                      $"\n{host}" +
                                      "\n\n/////////////////////////////////////////////////////\n");
                }
                else
                {
                    Console.WriteLine("\n/////////////////////////////////////////////////////" +
                                      "\n\nOverwatch | Help:" +
                                      "\n\now update: Updates Overwatch" +
                                      "\nrestart-all: Restarts the server" +
                                      "\ndeploy: Deploys the newest build of the website to the servers" +
                                      "\non: If Overwatch is on" +
                                      "\nlist: Lists all running servers and proxies" +
                                      "\n\n/////////////////////////////////////////////////////\n");
                }
            }
        }
    }
}