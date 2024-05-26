using System.Numerics;

namespace CSharp_Kubernetes.Proxy;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

class ReverseProxy
{
    private const bool AllowOtherListenPorts = true;
    
    private int _listenPort;
    private const string TargetHost = "localhost";
    
    private readonly List<int> ListenPorts = new List<int> { 80, 8080, 443 };
    private const int FallbackListenPort = 443;
    private const int FallBackTargetPort = 3000;
    
    private const int SSL_PORT = 443;
    private const int HTTP_PORT = 80;

    public ReverseProxy(int listenPort)
    {
        _listenPort = listenPort != ProxyServerHandler.ROOT_SERVER_PORT &&
                      (AllowOtherListenPorts || ListenPorts.Contains(listenPort)) ? listenPort : FallbackListenPort;
    }

    private int getNewTargetPort()
    {
        // Prefer HTTP Servers on HTTP Port 80, in all other cases prefer HTTPS Servers.
        if (_listenPort == HTTP_PORT)
        {
            int port = ProxyServerHandler.getNextHTTPPort();
            if (port == -1)
            {
                port = ProxyServerHandler.getNextSSLPort();
                Console.Error.WriteLine("Warning: No HTTP Backend Server Connections available at this time. Defaulting to HTTPS Server (will be empty response)...");
            }
            if (port == -1)
            {
                Console.Error.WriteLine("Error: No Backend Server Connections available at this time.");
            }
            return port;
        } else {
            int port = ProxyServerHandler.getNextSSLPort();
            if (port == -1)
            {
                port = ProxyServerHandler.getNextHTTPPort();
                Console.Error.WriteLine("Warning: No HTTPS Backend Server Connections available at this time. Defaulting to HTTP Server...");
            }
            if (port == -1)
            {
                Console.Error.WriteLine("Error: No Backend Server Connections available at this time.");
            }
            return port;
        }
    }
    
    public async Task Start()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, _listenPort);
        listener.Start();
        Console.WriteLine($"Listening on port {_listenPort}...");

        while (ProxyServerHandler.ProxyRunning)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            int targetPort = getNewTargetPort();
            
            // Increase Current Request Count
            if (!ProxyServerHandler.PortActiveRequests.ContainsKey(targetPort))
                ProxyServerHandler.PortActiveRequests.Add(targetPort, BigInteger.One);
            else ProxyServerHandler.PortActiveRequests[targetPort]++;
            
            NetworkStream clientStream = client.GetStream();
            TcpClient targetClient = new TcpClient();
            await targetClient.ConnectAsync(TargetHost, targetPort);
            NetworkStream targetStream = targetClient.GetStream();

            Task clientToTarget = RelayDataAsync(clientStream, targetStream);
            Task targetToClient = RelayDataAsync(targetStream, clientStream);

            await Task.WhenAny(clientToTarget, targetToClient);
            targetClient.Close();
            
            // Decrease Current Request Count
            ProxyServerHandler.PortActiveRequests[targetPort]--;
        }
    }

    private async Task RelayDataAsync(NetworkStream fromStream, NetworkStream toStream)
    {
        byte[] buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = await fromStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await toStream.WriteAsync(buffer, 0, bytesRead);
        }
    }
}