using System.Diagnostics;

namespace OW_Bonfire.Bonfire;

public class Bonfire
{
    private static int _overwatchId = -1;
    private static Dictionary<string, string> _arguments;
    
    public static async Task<int> Launch(string[] args)
    {
        _arguments = new();
        ParseArgs(args);
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
            default:
                _arguments[arg] = value;
                break;
        }
    }
}