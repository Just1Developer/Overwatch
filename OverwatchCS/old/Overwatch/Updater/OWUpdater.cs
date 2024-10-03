using System.Diagnostics;
using System.IO.Compression;
using CSharp_Kubernetes.Proxy;

namespace CSharp_Kubernetes.Overwatch.Updater;

public static class OWUpdater
{
    private const string latestRelease = "https://github.com/Just1Developer/Overwatch/releases/latest";
    private static string dotExe = Executable.IS_WINDOWS ? ".exe" : "";
    private static string fileNameThis = $"Overwatch";
    private static string downloadLink =
        $"https://github.com/Just1Developer/Overwatch/releases/latest/download/Overwatch-release-{(Executable.IS_WINDOWS ? "win" : "unix")}.zip";

    public static async Task UpdateOverwatch()
    {
        Manifest manifest = Manifest.Load();
        var version = manifest["version"];
        var link = manifest["link"];

        // Download new file
        string zipArchiveLoc = "update/overwatch-1.zip";    // Todo also download
        
        // Proceed with update
        
        int i = 0;
        while (File.Exists($"update/{fileNameThis}-backup-{i}{dotExe}")) ++i;
        string backupFileName = $"update/{fileNameThis}-backup-{i}{dotExe}";
        File.Copy(fileNameThis+dotExe, backupFileName);
        
        // -------------------------------------------------------------------------------------------------
        // |                                                                                               |
        // |   Start Detached Bonfire Instance with current, backup and update file name                   |
        // |   Bonfire will delete the current, copy the update and then delete or move back the backup.   |
        // |   Then, Bonfire will start Overwatch and terminate.                                           |
        // |                                                                                               |
        // |   After a successful update, Bonfire should also update the manifest file.                    |
        // |   Best case: The new updates comes with the new manifest file.                                |
        // |                                                                                               |
        // -------------------------------------------------------------------------------------------------

        string updateExtractionDir = "/update/newest-0/"; // Todo
        Directory.CreateDirectory(updateExtractionDir);
        ZipFile.ExtractToDirectory(zipArchiveLoc, updateExtractionDir);
        File.Delete(zipArchiveLoc);
        // Now there are now the following files in /update/newest-X/: overwatch[.exe] (executable), manifest
        
        string bonfireArgs = $"-c ./bonfire -updateLoc={updateExtractionDir}" +
                                    $" -https={ProxyServerHandler.SSLSecurePorts.Count}" +
                                    $" -http={ProxyServerHandler.InsecurePorts.Count}" +
                                    $" -root={ProxyServerHandler.ROOT_SERVER_PORT}";

        var process = Executable.GetProcess(Executable.GetShellStartInfo("./", bonfireArgs));
        process.Start();
        
        // to justify async currently
        await Task.Delay(50);
    }
    
    /*
     * Bash Script:
     * # Step 1: Backup the current application
       echo "Backing up current application..."
       cp -r "$APP_DIR" "$BACKUP_DIR"

       # Step 2: Download the latest release
       echo "Downloading the latest release..."
       curl -L "$DOWNLOAD_URL" -o /tmp/latest-release.zip

       # Step 3: Extract the new release
       echo "Extracting the latest release..."
       unzip -o /tmp/latest-release.zip -d /tmp/latest-release

       # Step 4: Replace the old files with the new ones
       echo "Updating application files..."
       cp -r /tmp/latest-release/* "$APP_DIR/"
     */
}