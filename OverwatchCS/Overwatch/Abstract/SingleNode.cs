namespace CSharp_Kubernetes.Overwatch.Abstract;

/**
 * A single node.
 * @author JustOneDeveloper
 */
public abstract class SingleNode
{
    /**
     * The port this node ist hosted on.
     */
    public int Port { get; private set; }

    /**
     * Launches the node. Can be embedded in the constructor when implemented.
     */
    public abstract void Launch();
    
    /**
     * Waits for all requests to finish and shuts down the node.
     */
    public abstract void Shutdown();
    
    /**
     * Kills the node instantly, without waiting for requests to finish.
     */
    public abstract void Kill();
    
    /**
     * The amount of open requests this node is currently processing.
     */
    public abstract void GetOpenRequests();
}