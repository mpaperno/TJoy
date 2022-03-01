
namespace TouchPortalSDK.Messages.Commands
{
    public class ConnectorUpdateShortCommand : ConnectorUpdateCommandBase
    {
        public string ShortId { get { return Id; } set { Id = value; } }

        public ConnectorUpdateShortCommand(string pluginId, string shortId, int value) : base(pluginId, shortId, value) {

        }
    }
}
