
namespace TouchPortalSDK.Messages.Commands
{
    public class ConnectorUpdateCommand : ConnectorUpdateCommandBase
    {
        public string ConnectorId { get { return Id; } set { Id = value; } }

        public ConnectorUpdateCommand(string pluginId, string connectorId, int value) : base(pluginId, $"pc_{pluginId}_{connectorId}", value)
        {

        }
    }
}
