using System.Diagnostics;
using CSharp_Kubernetes.Overwatch;
using CSharp_Kubernetes.Proxy;

namespace CSharp_Kubernetes.Deployment;

public static class ProdBuilder
{
    /// <summary>
    /// Updates the production build.
    /// </summary>
    public static async Task<bool> UpdateProductionBuild()
    {
        // Step 1: If existent, back up directory.
        string repopath = Executable.GetRelativeRepoPath(); // Ends with '/'
        if (Directory.Exists(repopath + ".next"))
        {
            GenerateProdBackup(repopath);
        }

        Process process = Executable.GetProcess(Executable.GetWebserverCmdInfo("git pull"));
        await Executable.RunProcessAsync(process,
            "GIT PULL", true);
        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine("Git pull failed, aborting prod update since no new version exists.");
            await RestoreOldProductionBuild(repopath);
            return false;
        }

        await Executable.RunProcessAsync(Executable.GetProcess(Executable.GetWebserverCmdInfo("pnpm i")),
            "PNPM INSTALL", true);

        await Executable.RunProcessAsync(Executable.GetProcess(Executable.GetWebserverCmdInfo("pnpm update")),
            "PNPM UPDATE", true);

        Process builder = Executable.GetProcess(Executable.GetWebserverCmdInfo("pnpm build"));
        await Executable.RunProcessAsync(builder,
            "PNPM BUILD", true);
        if (builder.ExitCode != 0)
        {

            // Build Failed! Let's delete node_modules and try again

            Console.Error.WriteLine("Build failed, deleting node_modules and package locks and retrying...");

            await ClearNodeModules(repopath);

            await Executable.RunProcessAsync(Executable.GetProcess(Executable.GetWebserverCmdInfo("pnpm i")),
                "PNPM INSTALL", true);

            builder = Executable.GetProcess(Executable.GetWebserverCmdInfo("pnpm build"));
            await Executable.RunProcessAsync(builder,
                "PNPM BUILD", true);
            if (builder.ExitCode != 0)
            {
                Console.Error.WriteLine("Build failed with reinstalling node_modules aborting update.");
                await RestoreOldProductionBuild(repopath);
                return false;
            }
        }

        // Working production build exists now.
        return true;
    }

    /// <summary>
    /// If existent, generates a backup of the current production build.
    /// Folders are named such that the folders are sorted chronologically
    /// when sorting alphabetically.
    /// Folder Format:
    /// Inside parent folder of Repository: /backup/%time%/prod/...
    /// </summary>
    /// <param name="repopath">The repository path.</param>
    private static void GenerateProdBackup(string repopath)
    {
        if (!Directory.Exists(repopath + ".next")) return;
        string targetFolder = repopath + "../backup/" + DateTime.Now.ToString("yyyy-MM-dd_tt.hh:mm:ss") + "/";
        Directory.CreateDirectory(targetFolder);
        Directory.Move(repopath + ".next", $"{targetFolder}prod/");
    }

    /// <summary>
    /// Deletes the node_modules folder.
    /// </summary>
    private static async Task ClearNodeModules(string repopath)
    {
        var path = $"{repopath}node_modules";
        Directory.Delete(path);
        await ProxyServerHandler.WaitFor(() => !Directory.Exists(path));
    }

    /// <summary>
    /// Restores the newest old production build.
    /// </summary>
    private static async Task RestoreOldProductionBuild(string repopath)
    {
        var targetFolder = Directory.GetDirectories(repopath + "../backup/");
        if (targetFolder.Length == 0)
        {
            Console.Error.WriteLine("Error: No old Prod Backup available");
            return;
        }

        string path;
        int i = 0;
        do
        {
            path = $"{targetFolder[i++]}/prod/";
        } while (!Directory.Exists(path) && i < targetFolder.Length);

        if (!Directory.Exists(path))
        {
            Console.Error.WriteLine("Error: No old Prod Backup available");
            return;
        }
        
        Directory.Move(path, repopath + ".next");
        path = $"{path}..";
        Directory.Delete(path, true);
        await ProxyServerHandler.WaitFor(() => !Directory.Exists(path));
    }

    // FROM: https://stackoverflow.com/a/3822913
    private static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
        //Now Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }

    public static void _GenerateProdBackup(string repopath)
    {
        GenerateProdBackup(repopath);
    }
    public static async Task _RestoreOldProductionBuild(string repopath)
    {
        await RestoreOldProductionBuild(repopath);
    }

}