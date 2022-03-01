namespace TouchPortalSDK.Interfaces
{
    /// <summary>
    /// Factory interface for creating a Touch Portal client.
    /// </summary>
    public interface ITouchPortalClientFactory
    {
        /// <summary>
        /// Create a Touch Portal Client
        /// </summary>
        /// <param name="eventHandler">Handler the events from Touch Portal, normally the plugin instance.</param>
        /// <returns>Touch Portal Client</returns>
        ITouchPortalClient Create(ITouchPortalEventHandler eventHandler);
    }
}