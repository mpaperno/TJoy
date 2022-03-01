using System;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Models;

namespace TouchPortalSDK.Messages.Commands
{
    public class CreateStateCommand : ITouchPortalMessage
    {
        public string Type => "createState";

        public string Id { get; set; }

        public string Desc { get; set; }

        public string DefaultValue { get; set; }

        public CreateStateCommand(string stateId, string desc, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(stateId))
                throw new ArgumentNullException(nameof(stateId));

            if (string.IsNullOrWhiteSpace(desc))
                throw new ArgumentNullException(nameof(desc));

            Id = stateId;
            Desc = desc;
            DefaultValue = defaultValue ?? string.Empty;
        }

        /// <inheritdoc cref="ITouchPortalMessage" />
        public Identifier GetIdentifier()
            => new Identifier(Type, Id, default);
    }
}
