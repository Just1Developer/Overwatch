using CSharp_Kubernetes.Proxy;

namespace CSharp_Kubernetes.Overwatch;

public class Overwatch
{
    public static async Task Launch()
    {
        await ProxyServerInit.StartReverseProxies();
    }
}