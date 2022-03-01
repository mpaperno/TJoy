namespace TouchPortalSDK.Interfaces
{
    public interface ITouchPortalSocket
    {
        /// <summary>
        /// The connection state of the Socket being used internally.
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Size of the receive buffer in bytes, for possible tuning.
        /// Needs to hold at least one full TP message at a time, but should not be too large
        /// since the unused space is still moved around in memory during line splitting process.
        /// This must be set <b>before</b> <see cref="Listen()"/> is called.
        /// Default is 2048.
        /// </summary>
        public int ReceiveBufferSize { get; set; }

        /// <summary>
        /// Connects to Touch Portal.
        /// </summary>
        /// <returns>success flag</returns>
        bool Connect();

        /// <summary>
        /// Starts the listener thread, and listens for events from Touch Portal.
        /// </summary>
        /// <returns>success flag</returns>
        bool Listen();

        /// <summary>
        /// Sends a string message to Touch Portal. The string is encoded to UTF8 before sending.
        /// </summary>
        /// <param name="jsonMessage">The fully formatted JSON to send.</param>
        /// <returns>success flag</returns>
        bool SendMessage(string jsonMessage);

        /// <summary>
        /// Sends a JSON message which is already encoded to a UTF8 byte array.
        /// </summary>
        /// <param name="messageBytes">UTF8 bytes of the fully formatted JSON to send.</param>
        /// <returns>success flag</returns>
        bool SendMessage(System.ReadOnlySpan<byte> messageBytes);

        /// <summary>
        /// Closes the socket.
        /// </summary>
        void CloseSocket();
    }
}
