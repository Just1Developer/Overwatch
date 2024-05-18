namespace CSharp_Kubernetes.Proxy;

public static class ProxyServerInit
{
    /**
     * The port on which runs the server with root login option.
     * Proxy must never be able do redirect to this port, it should
     * only be accessible on the root server or in the same network.
     */
    public static int ROOT_SERVER_PORT = 4000;

    const int HTTPPort = 80;
    const int HTTPSPort = 443;
    static int[] HTTPSTargets = { 3000 };
    static int[] HTTPTargets = { 3001 };
    
    
    public static bool ProxyRunning { get; private set; } = false;
    private static bool _tasksFinished = true;
    
    
    public static async Task StartReverseProxies()
    {
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
        Task httpsProxyTask = new ReverseProxy(HTTPSPort, HTTPSTargets).Start();

        // Create and start the HTTP proxy task
        Task httpProxyTask = new ReverseProxy(HTTPPort, HTTPTargets).Start();

        await Task.WhenAll(httpsProxyTask, httpProxyTask);
        _tasksFinished = true;
    }
}