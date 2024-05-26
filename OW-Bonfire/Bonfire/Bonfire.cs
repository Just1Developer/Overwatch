using System.Diagnostics;

namespace OW_Bonfire.Bonfire;

public class Bonfire
{
    private static int _overwatchId = -1;
    private static Dictionary<string, string> _arguments;
    private static bool IS_WINDOWS = false;
    private static string run_process_cmd;

    private static string newName, oldName;
    
    public static async Task<int> Launch(string[] args)
    {
        _arguments = new();
        ParseArgs(args);

        // Todo I don't think we need the suffix when executing via cmd
        string suffix = IS_WINDOWS ? ".exe" : "";
        newName = $"Overwatch_new{suffix}";
        oldName = $"Overwatch{suffix}";
        run_process_cmd = IS_WINDOWS ? run_process_win : run_process_unix;
        
        // Wait for overwatch to shut down before renaming the new files and relaunching overwatch
        if (_overwatchId >= 0)
        {
            Process p = Process.GetProcessById(_overwatchId);
            await p.WaitForExitAsync();
        }
        // Rename the files
        string file = "Overwatch";
        Process process = new Process();
        // Todo process
        Process? proc = Process.Start(file);
        if (proc == null)
        {
            Console.Error.WriteLine($"[FATAL] [{DateTime.Now}] Overwatch could not be started!");
            return 1;
        }
        return 0;
    }
    
    internal static ProcessStartInfo GetCmdInfo(string workingDirectory, string run_process_cmd, string arguments)
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

    private static void ParseArgs(string[] args)
    {
        foreach (string arg in args)
        {
            if (arg.Contains('='))
            {
                var a = arg.Split('=');
                SaveArg(a[0], a[1]);
            }
            else
            {
                SaveArg(arg, "true");
            }
        }
    }

    private static void SaveArg(string arg, string value)
    {
        switch (arg)
        {
            case "processId":
                _overwatchId = int.Parse(value);
                break;
            case "windows":
                IS_WINDOWS = true;
                break;
            default:
                _arguments[arg] = value;
                break;
        }
    }
}