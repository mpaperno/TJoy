using System;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Models;

namespace TouchPortalSDK.Messages.Commands
{
    public class SettingUpdateCommand : ITouchPortalMessage
    {
        public string Type => "settingUpdate";

        public string Name { get; set; }

        public string Value { get; set; }

        public SettingUpdateCommand(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            Name = name;
            Value = value ?? string.Empty;
        }

        /// <inheritdoc cref="ITouchPortalMessage" />
        public Identifier GetIdentifier()
            => new Identifier(Type, Name, default);
    }
}
