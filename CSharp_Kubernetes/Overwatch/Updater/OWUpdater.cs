using CSharp_Kubernetes.Proxy;

namespace CSharp_Kubernetes.Overwatch.Updater;

public static class OWUpdater
{
    private const string latestRelease = "https://github.com/Just1Developer/Overwatch/releases/latest";
    private static string downloadLink =
        $"https://github.com/Just1Developer/Overwatch/releases/latest/download/Overwatch-release-{(Executable.IS_WINDOWS ? "win" : "unix")}.zip";

    public static async Task UpdateOverwatch()
    {
        Manifest manifest = Manifest.Load();
        var version = manifest["version"];
        var link = manifest["link"];

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