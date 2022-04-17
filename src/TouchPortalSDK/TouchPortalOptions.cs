namespace TouchPortalSDK
{
    public class TouchPortalOptions
    {
        public string IpAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 12136;
        public static char ActionDataIdSeparator { get; set; } = '\0';
    }
}
