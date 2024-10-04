<div style="display: flex; justify-content: center;">
  <img src="https://github.com/Just1Developer/Overwatch/blob/main/OverwatchCS/_img/ow11.png?raw=true" alt="Overwatch" width="45%">
</div>

---

# Overwatch is currently undergoing a complete remodel and rewrite. The information provided below may be outdated or inaccurate.

---

Overwatch is a .NET-based server backend supporting 24/7 uptime with a kubernetes-inspired infrastructure.

Overwatch also supports updating itself and the production build with no downtime, handling HTTPS and HTTP requests separately, and provides optional hosting of a private root-server instance.

## Important
Updating, configurations files and complex proxy management are not yet implemented. The readme will be updated accordingly when features are implemented.

# General Architecture
For the specified number of https and http port, the specified amount of servers are started using pnpm start scripts. Overwatch then starts a reverse proxy on ports 443 and 80 and delegates requests to one of the servers started previously, requests via 443 to one of the https servers and port 80 to http servers, the main difference being that the https servers use an ssl encryption. Delegates from 443 to http servers and the other way around do not work. Overwatch will automatically keep track of all used ports and running server instances.

If a server fails to start, if existant, a backup script will be executed. Usually, this scripts starts the development build. This enables function even though certain parts of the production build won't compile or be executed at the time.

### ROOT Server Instance
If the server is equipped with a root script, Overwatch provides functionality to integrate the start of the root server as well. A root server is a server that, for example enables specific admin features. The proxy will not redirect, but instead block outside requests to that server, ensuring that the server is only accessable from the host itself.
Integrating the root server here will ensure it's included in any deployments, restarts and updates that are performed. It is recommended the root instance is always a development build as it's more important to always be somewhat available than it's access speed or caching.

---

# Updating
A restart of all servers can be invoked with the console command 'restart-all'. The restart process is structured in a way such that, as long as there is more than one server of each type (except root), the servers are restarted without downtime. A server restart performs the following steps for each server:
1. Remove the server from the list of receivers so that no more requests are directed at the server instance.
2. Wait until all pending requests are processed.
3. Shut down the server and wait until it's closed.
4. Start another server of the same type.
5. Wait until that server is fully up and ready to receive requests.

First, the HTTPS servers will be restarted, then the HTTP servers and lastly the root instance.

### Command Syntax: restart-all [OPTIONS] with Options: [-ssl=amount] [-http=amount] [-root=true/false]

It is possible to specify how many servers of each type should remain after the restart. If _n_ less servers than before are specified, the last _n_ servers will shut down but not restart. If _n_ more servers than before are specified, _n_ more servers will be started after restarting all servers of the given type.

---

# Deployment
Overwatch provides functionality to deploy the latest release of the web application without downtime. When deploying the web application, the following series of actions are performed:
1. *_'git pull'_* is executed inside the repository folder. If git is not configured in the CLI, this may prompt you to enter a username or password, you are however free to cancel and configure git in your terminal first.
2. The current production build located in /repository/.next/ is backed up to /backup/<time>/prod/.
3. *_'pnpm build'_* is executed in the repository folder.

   3.1. If the build fails, the deployment process is aborted and the old production build is loaded from the backup.

   3.2. If the build fails and the _-persistent_ flag is set to _true_, Overwatch will delete the ***node_modules*** folder and ***package-lock*** files and perform *'pnpm i'* to re-add all dependencies. If the build succeeds now, the deployment proceeds as normal. If not, see 3.1.
5. When the new production build is ready, the Overwatch will perform a full restart of all servers with all specified **ssl, http and root** arguments. See section about updating for further details.
6. The deployment is finished and marked as **successful**.

If the deployment is **successful**, the backup will be deleted unless the _-deleteOld_ flag is set to _false_.
If the deployment is **not successful**, the /.next/ folder will be deleted and the newest backup will be loaded from the backup folder into the repo as /.next/ folder.

### Command Syntax: deploy [OPTIONS] with Options: [-ssl=amount] [-http=amount] [-root=true/false] [-persistent=true/false] [-deleteOld=true/false]

---

# Updating Overwatch
Updating Overwatch is currently being implemented.
