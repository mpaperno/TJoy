using System;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Models;
using TouchPortalSDK.Messages.Models.Enums;

namespace TouchPortalSDK.Messages.Commands
{
    public class UpdateActionDataCommand : ITouchPortalMessage
    {
        public string Type => "updateActionData";

        public string InstanceId { get; set; }

        public DataValue Data { get; set; }

        public UpdateActionDataCommand(string dataId, double minValue, double maxValue, ActionDataType dataType, string instanceId = null)
        {
            if (string.IsNullOrWhiteSpace(dataId))
                throw new ArgumentNullException(nameof(dataId));
            
            Data = new DataValue(dataId, minValue, maxValue, dataType);

            if (!string.IsNullOrWhiteSpace(instanceId))
                InstanceId = instanceId;
        }

        public class DataValue
        {
            public string Id { get; set; }
            public double MinValue { get; set; }
            public double MaxValue { get; set; }
            public ActionDataType Type { get; set; }

            public DataValue(string dataId, double minValue, double maxValue, ActionDataType dataType)
            {
                if (string.IsNullOrWhiteSpace(dataId))
                    throw new ArgumentNullException(nameof(dataId));

                Id = dataId;
                MinValue = minValue;
                MaxValue = maxValue;
                Type = dataType;
            }
        }

        /// <inheritdoc cref="ITouchPortalMessage" />
        public Identifier GetIdentifier()
            => new Identifier(Type, Data.Id, InstanceId);
    }
}
