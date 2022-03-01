
namespace TouchPortalSDK.Messages.Events
{
    /// <inheritdoc cref="DataContainerEventBase" />
    public class ConnectorChangeEvent : DataContainerEventBase
    {
        /// <summary>
        /// The connector ID. Alias for DataContainerEventBase::Id.
        /// </summary>
        public string ConnectorId { get { return Id; } set { Id = value; } }

        /// <summary>
        /// Current value of the connector.
        /// </summary>
        public int Value { get; set; }
    }
}
