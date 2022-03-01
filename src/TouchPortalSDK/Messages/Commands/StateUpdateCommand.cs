using System;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Models;

namespace TouchPortalSDK.Messages.Commands
{
    public class StateUpdateCommand : ITouchPortalMessage
    {
        public string Type => "stateUpdate";

        public string Id { get; set; }

        public string Value { get; set; }

        public StateUpdateCommand(string stateId, string value)
        {
            if (string.IsNullOrWhiteSpace(stateId))
                throw new ArgumentNullException(nameof(stateId));

            Id = stateId;
            Value = value ?? string.Empty;
        }

        /// <inheritdoc cref="ITouchPortalMessage" />
        public Identifier GetIdentifier()
            => new Identifier(Type, Id, default);
    }
}