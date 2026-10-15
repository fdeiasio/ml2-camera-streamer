public abstract class StreamableObject
{
    /**
     * This is an interface for objects that can be streamed by the StreamManager.
     * It should define a method to get the data that will be streamed.
     * The StreamManager will call this method at the specified frame rate and 
     * send the data through the connection.
     */
    public abstract void Initialize();

    public abstract byte[] GetData(uint timeoutMilliseconds);
}