using System.Collections.Concurrent;
using System.Numerics;
using CSharp_Kubernetes.Overwatch;
using CSharp_Kubernetes.Proxy;

namespace CSharp_Kubernetes.Servers;

using System.Diagnostics;

public class Servers
{
    /// <summary>
    /// Root aint available (implemented) yet.
    /// </summary>
    private const bool ROOT_AVAILABLE = false;

    public const bool StartDevServerAlternative = true;
    private const string _script_ssl = "ssl";
    private const string _script_ssldev = "ssldev";
    private const string _script_root = "root";
    private const string _script_http = "http";
    private const string _script_httpdev = "httpdev";

    private static ConcurrentBag<Task> TaskPool = new ();

    private static string Command(string cmd) => $"pnpm {cmd}";
    
    /// <summary>
    /// Launches the internal Nextjs servers. Will always try to run a production build, if available.
    /// If no production build for the server type is available, developer builds will be started instead, if configured.
    /// (This feature does not yet exist)
    /// <br/>
    /// Server Types:
    /// HTTPS Server Script: pnpm ssl
    /// HTTP Server Script: pnpm start
    /// ROOT Server Script: pnpm root
    /// Fallback dev-build scripts:
    /// HTTPS Server Script: pnpm ssldev
    /// HTTP Server Script: pnpm dev
    /// ROOT Server Script: pnpm rdev
    /// <br/>
    /// Developer note: The exclusive root server protocol does not yet exist.
    /// </summary>
    /// <param name="httpsServers">The servers running with internal ssl certificate.</param>
    /// <param name="httpServers">The servers running without ssl certificate.</param>
    /// <param name="rootServer">The server that allows root login. Port will be locked from proxy. </param>
    /// <returns></returns>
    public static async Task LaunchServers(int httpsServers = 1, int httpServers = 0, bool rootServer = true)
    {
        // For Debugging / Testing paths:
        //runNewProcess("-c ls");
        //return Task.WhenAll(taskPool);
        
        for (int https = 0; https < httpsServers; ++https)
        {
            await LaunchSingular(script: _script_ssl, altScript: _script_ssldev);
        }
        for (int http = 0; http < httpServers; ++http)
        {
            await LaunchSingular(script: _script_http, altScript: _script_httpdev);
        }
        if (rootServer && ROOT_AVAILABLE)
        {
            await LaunchSingular(script: _script_root, "");
        }

        await Task.WhenAll(TaskPool);
    }

    internal static async Task LaunchSingular(ServerType type)
    {
        switch (type)
        {
            case ServerType.SSL:
                await LaunchSingular(script: _script_ssl, altScript: _script_ssldev);
                break;
            case ServerType.HTTP:
                await LaunchSingular(script: _script_http, altScript: _script_httpdev);
                break;
            case ServerType.ROOT:
                await LaunchSingular(script: _script_root, "");
                break;
            default:
                return;
        }
    }

    private static DateTime _lastDeployment = DateTime.Now;
    private static async Task LaunchSingular(string script, string altScript)
    {
        var old = _lastDeployment;
        TaskPool.Add(RunNewProcess(script: script, altScript: altScript));
        await ProxyServerHandler.WaitFor(() => old != _lastDeployment);
    }

    private static async Task RunNewProcess(string script, string altScript)
    {
        Process process = Executable.GetProcess(_startInfo(script));
        string str_port = "?";
        process.OutputDataReceived += async (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($">> [{script}#{str_port}]: {e.Data}");
                if (e.Data.Contains("Server ready on"))
                {
                    try
                    {
                        int port = int.Parse(e.Data.Split(':')[2]);
                        Console.WriteLine("Registering new port: " + port);
                        str_port = port.ToString();

                        bool success = false;
                        
                        if (e.Data.Contains("HTTPS")) success = ProxyServerHandler.AddSSLPort(port);
                        else if (e.Data.Contains("HTTP")) success = ProxyServerHandler.AddHTTPPort(port);
                        else if (e.Data.Contains("ROOT")) success = ProxyServerHandler.SetRootPort(port);
                        else Console.WriteLine("Failed to read server type in " + e.Data);

                        if (success)
                        {
                            _lastDeployment = DateTime.Now;
                            ProxyServerHandler.PortProcesses[port] = process;
                        }
                    }
                    catch (Exception ex)
                    {
                        string outp = e.Data ?? "<no message>";
                        Console.WriteLine($"Tried and failed to register port: {(outp.Contains(':') ? outp.Split(':')[2] : outp)} (registration failed).");
                        Console.WriteLine($"Outp: {outp}");
                        Console.WriteLine($"Error: {ex}");
                    }
                }
                else if (e.Data.Contains("Command failed with exit code") && StartDevServerAlternative && !string.IsNullOrEmpty(altScript))
                {
                    // => Starting failed, try again with alternative script. This is usually the dev script.
                    Console.WriteLine($"////////////////////////////////////////////////////////////////////\n" +
                                      $"Failed to start {script} Server. Switching to alt script {altScript}..." +
                                      $"\n////////////////////////////////////////////////////////////////////");
                    await RunNewProcess(script: altScript, altScript: "");
                    // This process will terminate anyway
                    // Todo: let this process die. Not necessary, so no pressing matter
                }
            }
        };

        // Don't add logging event, since we handle that separately.
        await Executable.RunProcessAsync(process, "", false);
    }

    private static ProcessStartInfo _startInfo(string script) =>
        Executable.GetWebserverCmdInfo(Command(script));

    private static ProcessStartInfo _debugStartInfo(string script, string workingDir) => 
        Executable.GetShellStartInfo(workingDir, script);
}