using System.Collections.Generic;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Models;

namespace TouchPortalSDK.Messages.Events
{
    public class InfoEvent : ITouchPortalMessage
    {
        /// <summary>
        /// Event from Touch Portal when a connection is established.
        /// This event includes information about the Touch Portal service.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Status ex. "paired"
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Version of the SDK this version of Touch Portal knows about.
        /// Ex. 2
        /// </summary>
        public int SdkVersion { get; set; }

        /// <summary>
        /// Touch Portal version as string.
        /// Major, Minor, Patch: M.m.ppp
        /// </summary>
        public string TpVersionString { get; set; }

        /// <summary>
        /// Touch Portal version as int.
        /// Format: Major * 10000 + Minor * 1000 + patch.
        /// </summary>
        public int TpVersionCode { get; set; }

        /// <summary>
        /// Plugin version as code.
        /// </summary>
        public int PluginVersion { get; set; }

        /// <summary>
        /// Values in settings.
        /// </summary>
        public IReadOnlyCollection<Setting> Settings { get; set; }

        /// <inheritdoc cref="ITouchPortalMessage" />
        public Identifier GetIdentifier()
            => new Identifier(Type, default, default);
    }
}
