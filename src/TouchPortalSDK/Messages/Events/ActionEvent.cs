using TouchPortalSDK.Messages.Models.Enums;

namespace TouchPortalSDK.Messages.Events
{
    /// <summary>
    /// <para>
    ///     Action type event. `Type` attribute can be one of the following:
    /// </para>
    /// <list type="bullet">
    /// <item>
    ///     <term>action</term>
    ///     <description>User presses an action button on the device.</description>
    /// </item>
    /// <item>
    ///     <term>down</term>
    ///     <description>Finger holds down the action on the device. This event happens only if the action enables the hasHoldFunctionality.</description>
    /// </item>
    /// <item>
    ///     <term>up</term>
    ///     <description>Finger released the action on the device. This event happens only if the action enables the hasHoldFunctionality.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <inheritdoc cref="DataContainerEventBase" />
    public class ActionEvent : DataContainerEventBase
    {
        /// <summary>
        /// The id of the action.
        /// </summary>
        public string ActionId { get { return Id; } set { Id = value; } }

        /// <summary>
        /// Returns the Action type.
        /// </summary>
        /// <returns><see cref="Press"/> enum</returns>
        public Press GetPressState()
            => Type switch
            {
                "up" => Press.Up,
                "down" => Press.Down,
                _ => Press.Tap
            };
    }
}
