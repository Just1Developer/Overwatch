using CSharp_Kubernetes.Proxy;
using CSharp_Kubernetes.Servers;

namespace CSharp_Kubernetes.Deployment;

public class ServerUpdater
{
    public static async Task UpdateServers()
    {
        await UpdateServers(ProxyServerHandler.SSLSecurePorts.Count, ProxyServerHandler.InsecurePorts.Count,
            ProxyServerHandler.ROOT_SERVER_PORT >= 0);
    }
    public static async Task UpdateServers(int targetHTTPS, int targetHTTP, bool rootServer)
    {
        Console.WriteLine();
        
        List<int> sslPorts = new List<int>(ProxyServerHandler.SSLSecurePorts);
        List<int> httpPorts = new List<int>(ProxyServerHandler.InsecurePorts);
        int rootPort = ProxyServerHandler.ROOT_SERVER_PORT;
        
        // Restart all
        
        // HTTPS Servers
        
        int i = 0;
        foreach (int port in sslPorts)
        {
            await RestartServer(port, i < targetHTTPS);
            i++;
        }
        while (i < targetHTTPS)
        {
            await Servers.Servers.LaunchSingular(ServerType.SSL);
            i++;
        }
        
        // HTTP Servers
        
        i = 0;
        foreach (int port in httpPorts)
        {
            await RestartServer(port, i < targetHTTP);
            i++;
        }
        while (i < targetHTTP)
        {
            await Servers.Servers.LaunchSingular(ServerType.HTTP);
            i++;
        }
        
        // Root Server Instance

        if (rootPort != -1)
        {
            await RestartServer(rootPort, rootServer);
        } else if (rootServer) await Servers.Servers.LaunchSingular(ServerType.ROOT);
    }

    private static async Task RestartServer(int port, bool restart)
    {
        if (restart) Console.WriteLine($"Restarting server running on port {port}.");
        else Console.WriteLine($"Shutting down server running on port {port}.");
        await ProxyServerHandler.RemoveAndShutdownPortAsync(port, restart);
    }
}