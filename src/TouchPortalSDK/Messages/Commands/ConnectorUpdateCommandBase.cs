using System;
using System.Linq;
using System.Text;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Models;

namespace TouchPortalSDK.Messages.Commands
{
    public abstract class ConnectorUpdateCommandBase : ITouchPortalMessage
    {
        public string Type => "connectorUpdate";

        public string Id { get; set; }

        public int Value { get; set; }

        public ConnectorUpdateCommandBase(string pluginId, string id, int value)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                throw new ArgumentNullException(nameof(pluginId));

            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));

            if (value < 0 || value > 100)
                throw new ArgumentException("Value must be between 0 and 100", nameof(value));

            Id = id;
            Value = value;
        }

        public Identifier GetIdentifier()
            => new Identifier(Type, Id, default);
    }
}
