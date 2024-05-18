using System.Collections.Concurrent;

namespace CSharp_Kubernetes.Servers;

using System.Diagnostics;

public class Servers
{
    /// <summary>
    /// Root aint available (implemented) yet.
    /// </summary>
    private const bool ROOT_AVAILABLE = false;
    private const bool IS_DEBUG_MODE = true;
    
    private const string run_process_win = "cmd.exe";
    private const string run_process_unix = "/bin/bash";

    // Windows versions are 0,1,2,3; 4 is unix, 5 is XBox, 6 is MacOS X, 7 is other
    private static readonly bool IS_WINDOWS = (int) Environment.OSVersion.Platform < 4;
    private static readonly string run_process_cmd = IS_WINDOWS ? run_process_win : run_process_unix;

    public static void PrintInfo()
    {
        Console.WriteLine($"OS Version: {Environment.OSVersion.Platform} ({(int) Environment.OSVersion.Platform})");
        Console.WriteLine($"Is Windows: {IS_WINDOWS}");
        Console.WriteLine($"run_process_cmd: {run_process_cmd}");
    }
    
    public const bool StartDevServerAlternative = true;
    private const string relativePathProd = @"/Streamy/";
    private const string absolutePathMacDebug = "../../../../../../WebstormProjects/StreamingService/";
    private const string absolutePathWinDebug = absolutePathMacDebug; // Todo
    private static readonly string workingDir = Debugger.IsAttached || IS_DEBUG_MODE ?
        (IS_WINDOWS ? absolutePathWinDebug : absolutePathMacDebug) : relativePathProd;

    private const string _script_ssl = "ssl";
    private const string _script_ssldev = "ssldev";
    private const string _script_root = "root";
    private const string _script_http = "http";

    private static string Command(string cmd) => $"-c \"pnpm {cmd}\"";
    
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
    public static Task LaunchServers(int httpsServers = 1, int httpServers = 0, bool rootServer = true)
    {
        ConcurrentBag<Task> taskPool = new ConcurrentBag<Task>();
        
        // For Debugging / Testing paths:
        //runNewProcess("-c ls");
        //return Task.WhenAll(taskPool);
        
        for (int https = 0; https < httpsServers; ++https)
        {
            taskPool.Add(RunNewProcess(script: _script_ssl));
        }
        for (int http = 0; http < httpServers; ++http)
        {
            taskPool.Add(RunNewProcess(script: _script_http));
        }
        if (rootServer && ROOT_AVAILABLE)
        {
            taskPool.Add(RunNewProcess(script: _script_root));
        }

        return Task.WhenAll(taskPool);
    }
    
    /*
     * 
       var t = new Thread(() =>
       {
           
       });
       t.Start();
     */

    private static async Task RunNewProcess(string script)
    {
        Process process = new Process();
        process.StartInfo = _startInfo(script);
        var tcs = new TaskCompletionSource<bool>();

        try
        {
            process.EnableRaisingEvents = true;  // Enable event raising
            process.Exited += (sender, args) =>
            {
                tcs.SetResult(true);  // Set the result when the process exits
                process.Dispose();  // Dispose the process object
            };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($">> [{script}]: {e.Data}");
                    if (e.Data.Contains("Server ready on"))
                    {
                        try
                        {
                            int port = int.Parse(e.Data.Split(':')[2]);
                            Console.WriteLine("Registering new port: " + port);
                        }
                        catch (Exception)
                        {
                            string outp = e.Data ?? "<no message>";
                            Console.WriteLine($"Tried and failed to register port: {(outp.Contains(':') ? outp.Split(':')[2] : outp)} (registration failed).");
                        }
                    }
                }
            };
            
            process.Start();
            process.BeginOutputReadLine();  // Begin asynchronous output reading
            Console.WriteLine("Starting process " + process.StartInfo.WorkingDirectory + "/" + process.StartInfo.FileName);
            
            await tcs.Task;
            Console.WriteLine("Process completed with exit code: " + process.ExitCode);
        }
        catch (Exception e)
        {
            Console.WriteLine("An error occurred: " + e.Message);
        }
    }

    private static ProcessStartInfo _startInfo(string script) => new ()
    {
        WorkingDirectory = workingDir,
        FileName = run_process_cmd,
        Arguments = Command(script),
        CreateNoWindow = true, // This prevents the command window from showing up
        UseShellExecute = false, // Necessary to redirect input/output if needed
        RedirectStandardOutput = true, // To capture the output if needed
    };

    private static ProcessStartInfo _debugStartInfo(string script, string workingDir) => new ()
    {
        WorkingDirectory = workingDir,
        FileName = run_process_cmd,
        Arguments = script,
        CreateNoWindow = true, // This prevents the command window from showing up
        UseShellExecute = false, // Necessary to redirect input/output if needed
        RedirectStandardOutput = true, // To capture the output if needed
    };
}