namespace CSharp_Kubernetes.Deployment;

public class WebDeployment
{
    public static async Task DeployNew()
    {
        /*
         * Deployment Process:
         * 
         * 1. Backup existing .next folder with current production build
         * 2. Update the repository from git
         * 3. Run the pnpm build command
           * 3.1. Failure
           * 3.2. Restore production build from backup and log failure.
           * 3.3. End of Deployment
         * 3.2. Success: Continue
         * 4. Copy list of running servers
         * 5. Restart servers one by one until list is empty
           * 5.1. Mark server to pause (not accept any more requests)
           * 5.2. Wait until server has processed all current requests
           * 5.3. Showdown server
           * 5.4. (If still under margin for online servers) Start server with same profile (ssl/http/root)
         * 6. Start additional servers, if number of servers is higher.
         * 7. Delete old production folder
         * 8. Mark deployment as complete and print success to console
         * 9. Deployment complete
         */
    }
}