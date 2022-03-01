using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Events;

namespace TouchPortalSDK.Configuration
{
    internal static class MessageResolver
    {
        // these enum names must match the corresponding TP message "type" which we support (case insensitive)
        internal enum TpEventName : byte
        {
            Unknown,
            // Events sent from TP
            Info,
            ClosePlugin,
            ListChange,
            Broadcast,
            Settings,
            Action,
            Down = Action,
            Up = Action,
            NotificationOptionClicked,
            ConnectorChange,
            ShortConnectorIdNotification,
            //// Commands sent to TP  (unused)
            // Pair,
            // ChoiceUpdate,
            // CreateState,
            // RemoveState,
            // StateUpdate,
            // UpdateActionData,
            // SettingsUpdate
        }

        // Map the event name enums to the corresponding types.  (Benchamrked against using an Attribute on the enums and a dict lookup is several magnitudes faster.)
        private static readonly ReadOnlyDictionary<TpEventName, System.Type> eventTypeMap = new ReadOnlyDictionary<TpEventName, System.Type>(
            new Dictionary<TpEventName, System.Type>() {
                { TpEventName.Action,                       typeof(ActionEvent) },
                { TpEventName.ListChange,                   typeof(ListChangeEvent) },
                { TpEventName.ConnectorChange,              typeof(ConnectorChangeEvent) },
                { TpEventName.Broadcast,                    typeof(BroadcastEvent) },
                { TpEventName.ShortConnectorIdNotification, typeof(ShortConnectorIdNotificationEvent) },
                { TpEventName.Settings,                     typeof(SettingsEvent) },
                { TpEventName.NotificationOptionClicked,    typeof(NotificationOptionClickedEvent) },
                { TpEventName.Info,                         typeof(InfoEvent) },
                { TpEventName.ClosePlugin,                  typeof(CloseEvent) },
            }
        );

        // Used for initial parsing of the json to determine the event type.
        private class TouchPortalMessageType
        {
            public TpEventName Type { get; set; } = TpEventName.Unknown;
        }

        /// <summary>
        /// Resolves and parses a JSON string from byte array into a <see cref="ITouchPortalMessage"/> event Type.
        /// </summary>
        /// <param name="message">byte array of UTF8-encdoed chars.</param>
        /// <returns>A resolved <see cref="ITouchPortalMessage"/> type or <c>null</c> if the event type is unknown.</returns>
        /// <exception cref="JsonException">In case of any JSON string parsing errors.</exception>
        internal static ITouchPortalMessage ResolveMessage(System.ReadOnlySpan<byte> message)
        {
            // Quickly parse just the message type. (~33% faster than parsing the full message into a generic JsonElement.)
            // Note: Let this throw on error... the consumer client can/should trap it and display the actual useful exception like parser error location.
            TouchPortalMessageType messageType = JsonSerializer.Deserialize<TouchPortalMessageType>(message, Options.JsonSerializerOptions);

            if (!eventTypeMap.TryGetValue(messageType.Type, out var eventType))
                return null;  // don't throw here, client will just pass the message json verbatim to the plugin

            // Finally deserialize the full message into an event Type.
            return (ITouchPortalMessage)JsonSerializer.Deserialize(message, eventType, Options.JsonSerializerOptions);
        }

    }
}
