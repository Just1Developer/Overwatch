using CSharp_Kubernetes.Deployment;
using CSharp_Kubernetes.Overwatch;
//await Overwatch.Launch();


// Testing:
string repopath = Executable.GetRelativeRepoPath();
if (Directory.Exists(repopath + ".next"))
{
    ProdBuilder._GenerateProdBackup(repopath);
    Console.WriteLine("Done.");
}

await ProdBuilder._RestoreOldProductionBuild(repopath);