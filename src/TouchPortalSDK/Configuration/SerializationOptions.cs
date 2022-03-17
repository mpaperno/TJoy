using System.Text.Json;
using System.Text.Json.Serialization;

namespace TouchPortalSDK.Configuration
{
    internal static class Options
    {
        internal static JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new SettingsConverter(),
                new ActionDataConverter()
            }
        };
    }
}
