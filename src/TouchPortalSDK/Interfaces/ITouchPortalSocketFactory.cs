namespace TouchPortalSDK.Interfaces
{
    /// <summary>
    /// Factory interface for creating a Touch Portal socket.
    /// </summary>
    public interface ITouchPortalSocketFactory
    {
        /// <summary>
        /// Create a Touch Portal Socket
        /// </summary>
        /// <param name="messageHandler">Handler the json events from the Socket, normally the client instance.</param>
        /// <returns>Touch Portal Socket</returns>
        ITouchPortalSocket Create(IMessageHandler messageHandler);
    }
}