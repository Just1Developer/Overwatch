using System.Diagnostics;
using System.Numerics;

namespace CSharp_Kubernetes.Proxy;

public static class ProxyServerHandler
{
    /**
     * The port on which runs the server with root login option.
     * Proxy must never be able do redirect to this port, it should
     * only be accessible on the root server or in the same network.
     *
     * Default: -1
     */
    public static int ROOT_SERVER_PORT { get; private set; } = -1;

    const int HTTPPort = 80;
    const int HTTPSPort = 443;

    internal static List<int> SSLSecurePorts = new ();
    internal static List<int> InsecurePorts = new();
    internal static int SSL_PTR = 0;
    internal static int HTTP_PTR = 0;
    
    internal static Dictionary<int, Process> PortProcesses = new ();
    internal static Dictionary<int, BigInteger> PortActiveRequests = new ();
    
    public static bool ProxyRunning { get; private set; } = false;
    private static bool _tasksFinished = true;
    
    
    public static async Task StartReverseProxies(int[]? SSLPorts = null, int[]? HTTPPorts = null)
    {
        if (SSLPorts == null) SSLPorts = new int[0];
        if (HTTPPorts == null) HTTPPorts = new int[0];
        
        foreach (int port in SSLPorts) AddSSLPort(port);
        foreach (int port in HTTPPorts) AddHTTPPort(port);
        
        // Later: Allow for proxy reboot
        if (ProxyRunning)
        {
            ProxyRunning = false;
            // Wait for all previous tasks to finish before initializing again
            while (!_tasksFinished) { }
        }
        ProxyRunning = true;
        _tasksFinished = false;
        

        Console.WriteLine("Starting Reverse Proxy");

        // Create and start the HTTPS proxy task
        Task httpsProxyTask = new ReverseProxy(HTTPSPort).Start();

        // Create and start the HTTP proxy task
        Task httpProxyTask = new ReverseProxy(HTTPPort).Start();

        await Task.WhenAll(httpsProxyTask, httpProxyTask);
        _tasksFinished = true;
    }

    public static void AddSSLPort(int port)
    {
        if (!SSLSecurePorts.Contains(port))
        {
            if (ROOT_SERVER_PORT != port)
            {
                SSLSecurePorts.Add(port);
                PortActiveRequests.Add(port, BigInteger.Zero);
                Console.WriteLine($"Added Port {port} to HTTPS proxy.");
            }
            else
            {
                Console.WriteLine($"Adding of port {port} denied: Port is ROOT-Host.");
            }
        }
        else
        {
            Console.WriteLine($"Adding of port {port} denied: Port is already in HTTPS proxy.");
        }
    }

    public static void AddHTTPPort(int port)
    {
        if (!InsecurePorts.Contains(port))
        {
            if (ROOT_SERVER_PORT != port)
            {
                InsecurePorts.Add(port);
                PortActiveRequests.Add(port, BigInteger.Zero);
                Console.WriteLine($"Added Port {port} to HTTP proxy.");
            }
            else
            {
                Console.WriteLine($"Adding of port {port} denied: Port is ROOT-Host.");
            }
        }
        else
        {
            Console.WriteLine($"Adding of port {port} denied: Port is already in HTTP proxy.");
        }
    }

    public static void SetRootPort(int port)
    {
        InsecurePorts.Remove(port);
        SSLSecurePorts.Remove(port);
        ROOT_SERVER_PORT = port;
        PortActiveRequests.Add(port, BigInteger.Zero);
    }

    /// <summary>
    /// Gets the next SSL port. Returns -1 if none is available.
    /// </summary>
    /// <returns></returns>
    internal static int getNextSSLPort()
    {
        if (SSLSecurePorts.Count == 0) return -1;
        if (SSL_PTR >= SSLSecurePorts.Count) SSL_PTR = 0;
        return SSLSecurePorts[SSL_PTR++];
    }

    /// <summary>
    /// Gets the next HTTP port. Returns -1 if none is available.
    /// </summary>
    /// <returns></returns>
    internal static int getNextHTTPPort()
    {
        if (InsecurePorts.Count == 0) return -1;
        if (HTTP_PTR >= InsecurePorts.Count) HTTP_PTR = 0;
        return InsecurePorts[HTTP_PTR++];
    }

    /// <summary>
    /// Removes the server with the given SSL port.
    /// Marks the server with that port as 
    /// </summary>
    internal static async Task RemoveAndShutdownPortAsync(int port, bool restart)
    {
        string type;
        if (ROOT_SERVER_PORT == port)
        {
            // Identify root
            type = "root";
        } else if (SSLSecurePorts.Remove(port))
        {
            type = "ssl";
        } else if (InsecurePorts.Remove(port))
        {
            type = "http";
        } else {
            Console.WriteLine($"Attempted to remove server on port {port}, but no such server was found.");
            return;
        }

        if (PortActiveRequests.ContainsKey(port))
        {
            // Port was now removed, so is accepting no more requests.
            // Now, wait until the requests are finished.
            var tcs = new TaskCompletionSource<bool>();
            while (!tcs.Task.IsCompleted)
            {
                if (PortActiveRequests[port] == BigInteger.Zero)
                    tcs.SetResult(true);
            }

            await tcs.Task;
            PortActiveRequests.Remove(port);
        }

        if (!PortProcesses.ContainsKey(port))
        {
            Console.WriteLine($"Attempted to shut down process for server running on port {port}, but no such process was found.");
            return;
        }
        
        PortProcesses[port].Kill();
        PortProcesses.Remove(port);

        if (!restart) return;
        // Restart server
        switch (type)
        {
            case "ssl":
                break;
            case "http":
                break;
            case "root":
                break;
            default:
                return;
        }
    }
}