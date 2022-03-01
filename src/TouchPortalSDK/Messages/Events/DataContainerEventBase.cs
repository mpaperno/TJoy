using System.Collections.Generic;
using System.Linq;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Models;

namespace TouchPortalSDK.Messages.Events
{
    /// <summary>
    /// Base class for events which have data members, such as actions and connectors.
    /// </summary>
    public abstract class DataContainerEventBase : ITouchPortalMessage
    {
        /// <summary>
        /// The Touch Portal event name.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The id of the plugin.
        /// </summary>
        public string PluginId { get; set; }

        /// <summary>
        /// The id of the action/connector.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Data is name/value pairs of options the user has selected for this action.
        /// Ex. data1: dropdown1
        ///     data2: dropdown2
        /// </summary>
        public IReadOnlyCollection<ActionDataSelected> Data { get; set; }

        /// <summary>
        /// Indexer to get data values.
        /// </summary>
        /// <param name="dataId">the id of the datafield.</param>
        /// <returns>the value of the data field as string or null if not exists</returns>
        public string this[string dataId]
            => GetValue(dataId);

        /// <summary>
        /// Returns the value of the selected item in an action data field.
        /// This value can be null in some cases, and will be null if data field is miss written.
        /// </summary>
        /// <param name="dataId">the id of the datafield.</param>
        /// <returns>the value of the data field as string or null if not exists</returns>
        public string GetValue(string dataId)
                => Data?.SingleOrDefault(data => data.Id == dataId)?.Value;

        public Identifier GetIdentifier()
            => new Identifier(Type, Id, default);
    }
}
