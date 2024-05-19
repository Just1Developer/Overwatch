using CSharp_Kubernetes.Proxy;
using CSharp_Kubernetes.Servers;

namespace CSharp_Kubernetes.Deployment;

public class ServerUpdater
{
    public static async Task UpdateServers(int targetHTTPS, int targetHTTP, bool rootServer)
    {
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
            Servers.Servers.LaunchSingular(ServerType.SSL);
            i++;
        }
        
        // HTTP Servers
        
        i = 0;
        foreach (int port in httpPorts)
        {
            await RestartServer(port, i < targetHTTP);
            i++;
        }
        while (i < targetHTTPS)
        {
            Servers.Servers.LaunchSingular(ServerType.HTTP);
            i++;
        }
        
        // Root Server Instance

        if (rootPort != -1)
        {
            await RestartServer(rootPort, rootServer);
        } else if (rootServer) Servers.Servers.LaunchSingular(ServerType.ROOT);
    }

    private static async Task RestartServer(int port, bool restart)
    {
        await ProxyServerHandler.RemoveAndShutdownPortAsync(port, restart);
    }
}