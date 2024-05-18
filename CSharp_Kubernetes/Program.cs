// See https://aka.ms/new-console-template for more information

using CSharp_Kubernetes;

const int HTTPPort = 80;
const int HTTPSPort = 443;
int[] DefaultTargetPorts = { 3000 };

Console.WriteLine("Starting Reverse Proxy");

// Test / Debug:
//await new ReverseProxy(80, DefaultTargetPorts).Start();


// Create and start the HTTPS proxy task
Task httpsProxyTask = new ReverseProxy(HTTPSPort, DefaultTargetPorts).Start();

// Create and start the HTTP proxy task
Task httpProxyTask = new ReverseProxy(HTTPPort, DefaultTargetPorts).Start();

await Task.WhenAll(httpsProxyTask, httpProxyTask);

//*/