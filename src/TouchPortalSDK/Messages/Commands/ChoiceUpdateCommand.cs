using System;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Models;

namespace TouchPortalSDK.Messages.Commands
{
    public class ChoiceUpdateCommand : ITouchPortalMessage
    {
        public string Type => "choiceUpdate";

        public string Id { get; set; }

        public string[] Value { get; set; }

        public string InstanceId { get; set; }

        public ChoiceUpdateCommand(string listId, string[] value, string instanceId = null)
        {
            if (string.IsNullOrWhiteSpace(listId))
                throw new ArgumentNullException(nameof(listId));
            
            Id = listId;
            Value = value ?? Array.Empty<string>();

            if (!string.IsNullOrWhiteSpace(instanceId))
                InstanceId = instanceId;
        }

        /// <inheritdoc cref="ITouchPortalMessage" />
        public Identifier GetIdentifier()
            => new Identifier(Type, Id, InstanceId);
    }
}