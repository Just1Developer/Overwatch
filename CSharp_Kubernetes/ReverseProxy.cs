namespace CSharp_Kubernetes;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

class ReverseProxy
{
    private const bool AllowOtherListenPorts = true;
    
    private int _listenPort;
    private int _targetPort;
    private const string TargetHost = "localhost";
    
    private readonly List<int> ListenPorts = new List<int> { 80, 8080, 443 };
    private const int FallbackListenPort = 443;
    private const int FallBackTargetPort = 3000;

    public ReverseProxy(int listenPort, int[]? targetPorts)
    {
        _listenPort = AllowOtherListenPorts || ListenPorts.Contains(listenPort) ? listenPort : FallbackListenPort;
        _targetPort = targetPorts != null && targetPorts.Length > 0 ? targetPorts[0] : FallBackTargetPort;
    }
    
    public async Task Start()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, _listenPort);
        listener.Start();
        Console.WriteLine($"Listening on port {_listenPort}...");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            NetworkStream clientStream = client.GetStream();
            TcpClient targetClient = new TcpClient();
            await targetClient.ConnectAsync(TargetHost, _targetPort);
            NetworkStream targetStream = targetClient.GetStream();

            Task clientToTarget = RelayDataAsync(clientStream, targetStream);
            Task targetToClient = RelayDataAsync(targetStream, clientStream);

            await Task.WhenAny(clientToTarget, targetToClient);
            targetClient.Close();
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