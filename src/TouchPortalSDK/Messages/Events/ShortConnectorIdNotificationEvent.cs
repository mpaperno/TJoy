using System.Collections.Generic;
using System.Linq;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Models;

namespace TouchPortalSDK.Messages.Events
{
    public class ShortConnectorIdNotificationEvent : ITouchPortalMessage
    {
        public string Type { get; set; }
        public string PluginId { get; set; }
        public string ConnectorId { get; set; }
        public string ShortId { get; set; }

        public Identifier GetIdentifier()
            => new Identifier(Type, ConnectorId, default);

  }
}
