using System.Diagnostics;

namespace CSharp_Kubernetes.Overwatch;

public static class Executable
{
    private const bool IS_DEBUG_MODE = false;
    
    private const string run_process_win = "cmd.exe";
    private const string run_process_unix = "/bin/bash";

    // Windows versions are 0,1,2,3; 4 is unix, 5 is XBox, 6 is MacOS X, 7 is other
    internal static readonly bool IS_WINDOWS = (int) Environment.OSVersion.Platform < 4;
    private static readonly string run_process_cmd = IS_WINDOWS ? run_process_win : run_process_unix;

    public static void PrintInfo()
    {
        Console.WriteLine($"OS Version: {Environment.OSVersion.Platform} ({(int) Environment.OSVersion.Platform})");
        Console.WriteLine($"Is Windows: {IS_WINDOWS}");
        Console.WriteLine($"run_process_cmd: {run_process_cmd}");
    }
    
    private const string relativePathProd = @"/home/dev/Desktop/Overwatch/Streamy/StreamingService/";
    private const string absolutePathMacDebug = "../../../../../../WebstormProjects/StreamingService/";
    private const string absolutePathWinDebug = absolutePathMacDebug; // Todo
    private static readonly string WebServerRelativePath = Debugger.IsAttached || IS_DEBUG_MODE ?
        (IS_WINDOWS ? absolutePathWinDebug : absolutePathMacDebug) : relativePathProd;

    internal static string GetRelativeRepoPath() => WebServerRelativePath;
    
    /// <summary>
    /// Gets a new Process with the given start info. The process has raising events enabled by default.
    /// </summary>
    /// <param name="startInfo">The start info</param>
    /// <returns>The new process.</returns>
    internal static Process GetProcess(ProcessStartInfo startInfo)
    {
        Process process = new Process();
        process.StartInfo = startInfo;
        process.EnableRaisingEvents = true;
        return process;
    }

    internal static async Task RunProcessAsync(Process process, string processName, bool logOutput)
    {
        var tcs = new TaskCompletionSource<bool>();

        try
        {
            process.Exited += (sender, args) =>
            {
                tcs.SetResult(true);  // Set the result when the process exits
                process.Dispose();  // Dispose the process object
            };

            if (logOutput)
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.WriteLine($">> [{processName}]: {e.Data}");
                };
            }

            process.Start();
            process.BeginOutputReadLine();  // Begin asynchronous output reading
            Console.WriteLine($"Starting process {processName} " + process.StartInfo.WorkingDirectory + process.StartInfo.FileName);
            
            await tcs.Task;
            Console.WriteLine($"Process {processName} completed with exit code: " + process.ExitCode);
        }
        catch (Exception e)
        {
            Console.WriteLine("An error occurred: " + e.Message);
        }
    }

    internal static void EndProcess(Process process)
    {
        if (IS_WINDOWS)
        {
            // Using PowerShell to send Ctrl+C
            var powershell = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"Stop-Process -Id {process.Id} -Force\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            powershell.Start();
            powershell.WaitForExit();
        }
        else
        {
            var killProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"kill -2 {process.Id}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            killProcess.Start();
            killProcess.WaitForExit();
        }
    }

    internal static ProcessStartInfo GetStartInfo(string workingDirectory, string processName, string arguments)
    {
        return new ProcessStartInfo()
        {
            WorkingDirectory = workingDirectory,
            FileName = processName,
            Arguments = arguments,
            CreateNoWindow = true, // This prevents the command window from showing up
            UseShellExecute = false, // Necessary to redirect input/output if needed
            RedirectStandardOutput = true, // To capture the output if needed
        };
    }

    internal static ProcessStartInfo GetCmdInfo(string workingDirectory, string arguments)
    {
        return new ProcessStartInfo()
        {
            WorkingDirectory = workingDirectory,
            FileName = run_process_cmd,
            Arguments = arguments,
            CreateNoWindow = true, // This prevents the command window from showing up
            UseShellExecute = false, // Necessary to redirect input/output if needed
            RedirectStandardOutput = true, // To capture the output if needed
        };
    }

    internal static ProcessStartInfo GetWebserverCmdInfo(string arguments)
    {
        return new ProcessStartInfo()
        {
            WorkingDirectory = WebServerRelativePath,
            FileName = run_process_cmd,
            Arguments = $"-c \"{arguments}\"",
            CreateNoWindow = true, // This prevents the command window from showing up
            UseShellExecute = false, // Necessary to redirect input/output if needed
            RedirectStandardOutput = true, // To capture the output if needed
        };
    }
}