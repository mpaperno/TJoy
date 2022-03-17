using System.Collections.Generic;
using System.Linq;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Models;
using TouchPortalSDK.Configuration;

namespace TouchPortalSDK.Messages.Events
{
    public class ShortConnectorIdNotificationEvent : ITouchPortalMessage
    {
        public string Type { get; set; }
        public string PluginId { get; set; }
        public string ConnectorId { get; set; }
        public string ShortId { get; set; }
        /// <summary>
        /// Data is name = value pairs dictionary of the connector data members extracted
        /// from the connectorId string.
        /// It is populated only when the Data or ActualConnectorId members are first requested (lazy loading),
        /// or can be pre-populated by calling the ParseData() method.
        /// </summary>
        public ActionData Data {
            get {
                if (_data == null)
                    ParseData();
                return _data;
            }
        }
        /// <summary>
        /// Contains the actual connector ID, which is the first part of the sent
        /// connectorId field, before the first "|" delimiter, and also with the
        /// "pc_pluginId_" prefix stripped out.
        /// It is populated only when the ActualConnectorId or Data are first requested (lazy loading),
        /// or can be pre-populated by calling the ParseData() method.
        /// </summary>
        public string ActualConnectorId {
            get {
                if (_data == null)
                    ParseData();
                return _parsedConnectorId;
            }
        }

        private ActionData _data = null;
        private string _parsedConnectorId = null;

        /// <summary>
        /// Parse the long connectorId string into key/value pairs of data fields it represents.
        /// Populates the Data and ActualConnectorId properties.
        /// </summary>
        public void ParseData()
        {
            var p = new ConnectorIdParser(ConnectorId, PluginId);
            _data = p.Data;
            _parsedConnectorId = p.ConnectorIdPart;
        }

        public Identifier GetIdentifier()
            => new Identifier(Type, ConnectorId, default);

  }
}
