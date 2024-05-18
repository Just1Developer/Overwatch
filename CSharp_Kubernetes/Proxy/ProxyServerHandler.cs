namespace CSharp_Kubernetes.Proxy;

public static class ProxyServerHandler
{
    /**
     * The port on which runs the server with root login option.
     * Proxy must never be able do redirect to this port, it should
     * only be accessible on the root server or in the same network.
     */
    public static int ROOT_SERVER_PORT { get; private set; } = 4000;

    const int HTTPPort = 80;
    const int HTTPSPort = 443;

    internal static List<int> SSLSecurePorts = new ();
    internal static List<int> InsecurePorts = new();
    internal static int SSL_PTR = 0;
    internal static int HTTP_PTR = 0;
    
    public static bool ProxyRunning { get; private set; } = false;
    private static bool _tasksFinished = true;
    
    
    public static async Task StartReverseProxies(int[]? SSLPorts = null, int[]? HTTPPorts = null)
    {
        if (SSLPorts == null) SSLPorts = new int[0];
        if (HTTPPorts == null) HTTPPorts = new int[0];
        
        SSLSecurePorts.AddRange(SSLPorts);
        InsecurePorts.AddRange(HTTPPorts);
        
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
        if(!SSLSecurePorts.Contains(port) && ROOT_SERVER_PORT != port) SSLSecurePorts.Add(port);
    }

    public static void AddHTTPPort(int port)
    {
        if(!InsecurePorts.Contains(port) && ROOT_SERVER_PORT != port) InsecurePorts.Add(port);
    }

    public static void SetRootPort(int port)
    {
        InsecurePorts.Remove(port);
        SSLSecurePorts.Remove(port);
        ROOT_SERVER_PORT = port;
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
}