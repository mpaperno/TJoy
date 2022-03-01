using TouchPortalSDK.Messages.Models;

namespace TouchPortalSDK.Interfaces
{
    public interface ITouchPortalMessage
    { 
        /// <summary>
        /// Type of the message, see Touch Portal API documentation.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Gets a unique identifier for a command/event.
        /// (Type, Id, Instance)
        /// </summary>
        /// <returns></returns>
        Identifier GetIdentifier();
    }
}
