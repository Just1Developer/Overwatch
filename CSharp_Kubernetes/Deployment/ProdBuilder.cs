using CSharp_Kubernetes.Overwatch;

namespace CSharp_Kubernetes.Deployment;

public static class ProdBuilder
{
    /// <summary>
    /// Updates the production build.
    /// </summary>
    public static async Task UpdateProductionBuild()
    {
        // Step 1: If existent, back up directory.
        string repopath = Executable.GetRelativeRepoPath();     // Ends with '/'
        await GenerateProdBackup(repopath);
    }

    /// <summary>
    /// If existent, generates a backup of the current production build.
    /// Folders are named such that the folders are sorted chronologically
    /// when sorting alphabetically.
    /// Folder Format:
    /// Inside parent folder of Repository: /backup/%time%/prod/...
    /// </summary>
    /// <param name="repopath">The repository path.</param>
    private static async Task GenerateProdBackup(string repopath)
    {
        if (!Directory.Exists(repopath + ".next")) return;
        string targetFolder = repopath + "../backup/" + DateTime.Now.ToString("yyyy-MM-dd_tt.hh:mm:ss") + "/prod/";
        Directory.CreateDirectory(targetFolder);
        await Task.Run(() => CopyFilesRecursively(repopath + ".next", targetFolder));
    }
    
    // FROM: https://stackoverflow.com/a/3822913
    private static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        //Now Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }
}