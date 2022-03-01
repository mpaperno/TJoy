using System;

namespace TouchPortalSDK.Interfaces
{
    public interface IMessageHandler
    {
        /// <summary>
        /// Method for handling raw messages events, in UTF8 encoded json byte array.
        /// </summary>
        /// <param name="message"></param>
        void OnMessage(byte[] message);

        /// <summary>
        /// Method for notifying the message handler of hard errors (handler should probably disconnect after this).
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        void OnError(string message, Exception exception);
    }
}
