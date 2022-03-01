using TouchPortalSDK.Messages.Models;
using TouchPortalSDK.Messages.Models.Enums;

namespace TouchPortalSDK.Interfaces
{
    public interface ICommandHandler
    {
        /// <summary>
        /// Send a custom command. There is no state tracking for this.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        bool SendMessage(string message);

        /// <summary>
        /// Creates a dynamic state in Touch Portal Memory.
        /// This state will disappear when restarting Touch Portal.
        /// You will need to persist them yourself and reload them on plugin load.
        /// </summary>
        /// <param name="stateId"></param>
        /// <param name="desc">Description of the created state (name in menus).</param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        bool CreateState(string stateId, string desc, string defaultValue = "");

        /// <summary>
        /// Updates a setting in Touch Portal.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool SettingUpdate(string name, string value = "");

        /// <summary>
        /// Removes the dynamic state from Touch Portal.
        /// </summary>
        /// <param name="stateId"></param>
        /// <returns></returns>
        bool RemoveState(string stateId);

        /// <summary>
        /// Value that can be displayed, or an event can trigger on.
        /// Values are not persisted, and will fallback to default value on restart.
        /// - Plugin: Defined in the Entry.tp
        /// - Dynamic: Created or removed at runtime. (in memory only)
        /// - Global: Defined in the Touch Portal UI. (state definition persisted in %AppData%\TouchPortal\states.tp)
        /// </summary>
        /// <param name="stateId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool StateUpdate(string stateId, string value = "");

        /// <summary>
        /// Updates the drop down choices in the Touch Portal UI.
        /// InstanceId can be used to dynamically update a dropdown based on the value chosen from another dropdown.
        /// </summary>
        /// <param name="choiceId">Id of UI dropdown.</param>
        /// <param name="values">Values as string array that you can choose from.</param>
        /// <param name="instanceId">if set (fetched from listChange event), this will only update this particular list.</param>
        /// <returns></returns>
        bool ChoiceUpdate(string choiceId, string[] values, string instanceId = default);

        /// <summary>
        /// Updates the constraints of a data value.
        /// </summary>
        /// <param name="dataId">Id of action the number box.</param>
        /// <param name="minValue">Min value the field can be.</param>
        /// <param name="maxValue">Max value the field can be.</param>
        /// <param name="dataType">Type of the data field.</param>
        /// <param name="instanceId">if set (fetched from listChange event), this will only update this particular list.</param>
        /// <returns></returns>
        bool UpdateActionData(string dataId, double minValue, double maxValue, ActionDataType dataType, string instanceId = default);

        /// <summary>
        /// Adds a notification to the Touch Portal UI. Ex. if the plugin has a updated version.
        /// </summary>
        /// <param name="notificationId">If of the notification.</param>
        /// <param name="title">Title on the notification shown to the user.</param>
        /// <param name="message">Text / description of the notification shown to the user.</param>
        /// <returns></returns>
        bool ShowNotification(string notificationId, string title, string message, NotificationOptions[] notificationOptions);

        /// <summary>
        /// Sends a connector value update to Touch Portal using the long form of the connector ID.
        /// </summary>
        /// <param name="connectorId">The long ID of the connector to update. The string "pc_{pluginId}_" is automatically prepended
        /// before sending to TP. The total length must not exceed 200 chars.</param>
        /// <param name="value">The value to send, must be between 0 and 100, inclusive.</param>
        /// <returns>true on success, false otherwise.</returns>
        bool ConnectorUpdate(string connectorId, int value);

        /// <summary>
        /// Sends a connector value update to Touch Portal using the short form of the connector ID.
        /// </summary>
        /// <param name="shortId">The short ID of the connector to update. This is obtained from a <see cref="ShortConnectorIdNotification"/> event.</param>
        /// <param name="value">The value to send, must be between 0 and 100, inclusive.</param>
        /// <returns>true on success, false otherwise.</returns>
        bool ConnectorUpdateShort(string shortId, int value);
    }
}
