namespace TouchPortalSDK.Messages.Models
{
    public readonly struct Identifier
    {
        public string Type { get; }
        public string Id { get; }
        public string InstanceId { get; }

        public Identifier(string type, string id, string instanceId)
        {
            Type = type;
            Id = id;
            InstanceId = instanceId;
        }
    }
}
