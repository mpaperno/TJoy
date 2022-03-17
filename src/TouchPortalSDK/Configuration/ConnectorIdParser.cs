using System;
using System.Collections.Generic;
using System.Linq;
using TouchPortalSDK.Messages.Models;

namespace TouchPortalSDK.Configuration
{
    /// <summary>
    /// Parses a "long"  connectorId string from a ConnectorShortIdNotification message into a
    /// dict of key value pairs.
    /// </summary>
    /// Example connectorId:
    /// pc_pluginID_connectorId|actionDataId1=dataValue1|actionDataId2=dataValue2
    /// where the "pc_pluginID_" part is prepended by TP to the actual connectorId.
    /// This helper will first split split the string on the pipe delimeter,
    /// and then further into key/value pairs split on the equals sign.
    /// The result of the parsing (if any) is available from the Data property.
    /// The first member of the initial split on the pipe char is saved in the
    /// ConnectorIdPart property. If a pluginId is passed to the class c'tor, then
    /// the leading "pc_pluginId_" part will be stripped out of the ConnectorIdPart result.
    class ConnectorIdParser
    {
        public string ConnectorIdPart { get; }
        public ActionData Data => _kvPairs;

        private readonly ActionData _kvPairs = new ActionData();

        /// <param name="connectorId"></param>
        /// <param name="pluginId"></param>
        public ConnectorIdParser(string connectorId, string pluginId = null)
        {
            var values = connectorId?.Split('|', StringSplitOptions.RemoveEmptyEntries);
            // get our actual connector id
            ConnectorIdPart = values.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(ConnectorIdPart))
                return;

            if (!string.IsNullOrWhiteSpace(pluginId))
                ConnectorIdPart.Replace("pc_" + pluginId + "_", "");

            for (int i = 1, e = values.Length; i < e; ++i) {
                var keyVal = values[i].Split('=', StringSplitOptions.RemoveEmptyEntries);
                var len = keyVal.Length;
                if (len == 2)
                    _kvPairs.Add(keyVal[0], keyVal[1]);
                else if (len == 1)
                    _kvPairs.Add(keyVal[0], "");
                else if (len > 2)
                    _kvPairs.Add(keyVal[0], string.Join('=', keyVal[1..^0]));
            }
        }
    }
}
