using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Models;

namespace TouchPortalSDK.Messages.Events
{
    public class NotificationOptionClickedEvent : ITouchPortalMessage
    {
        /// <summary>
        /// Notification option in Touch Portal UI clicked by a user.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Id of the notification.
        /// </summary>
        public string NotificationId { get; set; }

        /// <summary>
        /// The option clicked by the user.
        /// </summary>
        public string OptionId { get; set; }

        /// <inheritdoc cref="ITouchPortalMessage" />
        public Identifier GetIdentifier()
            => new Identifier(Type, NotificationId, default);
    }
}
