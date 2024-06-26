using System.Diagnostics;
using System.Numerics;
using CSharp_Kubernetes.Overwatch;
using CSharp_Kubernetes.Servers;

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

    public static bool AddSSLPort(int port)
    {
        if (!SSLSecurePorts.Contains(port))
        {
            if (ROOT_SERVER_PORT != port)
            {
                SSLSecurePorts.Add(port);
                if (!PortActiveRequests.ContainsKey(port)) PortActiveRequests.Add(port, BigInteger.Zero);
                Console.WriteLine($"Added Port {port} to HTTPS proxy.");
                return true;
            }
            // else:
            Console.WriteLine($"Adding of port {port} denied: Port is ROOT-Host.");
        }
        else
        {
            Console.WriteLine($"Adding of port {port} denied: Port is already in HTTPS proxy.");
        }
        return false;
    }

    public static bool AddHTTPPort(int port)
    {
        if (!InsecurePorts.Contains(port))
        {
            if (ROOT_SERVER_PORT != port)
            {
                InsecurePorts.Add(port);
                if (!PortActiveRequests.ContainsKey(port)) PortActiveRequests.Add(port, BigInteger.Zero);
                Console.WriteLine($"Added Port {port} to HTTP proxy.");
                return true;
            }
            // else:
            Console.WriteLine($"Adding of port {port} denied: Port is ROOT-Host.");
        }
        else
        {
            Console.WriteLine($"Adding of port {port} denied: Port is already in HTTP proxy.");
        }
        return false;
    }

    public static bool SetRootPort(int port)
    {
        InsecurePorts.Remove(port);
        SSLSecurePorts.Remove(port);
        ROOT_SERVER_PORT = port;
        if (!PortActiveRequests.ContainsKey(port)) PortActiveRequests.Add(port, BigInteger.Zero);
        return true;
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
        ServerType type;
        if (ROOT_SERVER_PORT == port)
        {
            // Identify root
            type = ServerType.ROOT;
        } else if (SSLSecurePorts.Remove(port))
        {
            type = ServerType.SSL;
        } else if (InsecurePorts.Remove(port))
        {
            type = ServerType.HTTP;
        } else {
            Console.WriteLine($"Attempted to remove server on port {port}, but no such server was found.");
            return;
        }

        if (PortActiveRequests.ContainsKey(port))
        {
            // Port was now removed, so is accepting no more requests.
            // Now, wait until the requests are finished.
            await WaitFor(() => PortActiveRequests[port] == BigInteger.Zero);
            PortActiveRequests.Remove(port);
        }

        if (!PortProcesses.ContainsKey(port))
        {
            Console.WriteLine($"Attempted to shut down process for server running on port {port}, but no such process was found.");
            return;
        }

        //await PortProcesses[port].StandardInput.WriteLineAsync("^C");
        Console.WriteLine("huh");
        Executable.EndProcess(PortProcesses[port]);
        
        Console.WriteLine("Awaiting Shutdown: " + DateTime.Now);
        //await WaitFor(() => PortProcesses[port].HasExited);
        Console.WriteLine("Process has exited: " + DateTime.Now);
        PortProcesses.Remove(port);
        Console.WriteLine($"Server from port {port} shut down.");

        if (!restart) return;
        // Restart server
        await Servers.Servers.LaunchSingular(type);
        Console.WriteLine($"Server from port {port} restarted.");
    }

    internal static async Task WaitFor(Func<bool> waitForFunction)
    {
        var tcs = new TaskCompletionSource<bool>();
        while (!tcs.Task.IsCompleted)
        {
            if (waitForFunction.Invoke())
                tcs.SetResult(true);
        }

        await tcs.Task;
    }
}