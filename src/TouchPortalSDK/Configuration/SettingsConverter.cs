using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using TouchPortalSDK.Messages.Models;

namespace TouchPortalSDK.Configuration
{
    internal class SettingsConverter : JsonConverter<IReadOnlyCollection<Setting>>
    {
        public override IReadOnlyCollection<Setting> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dictionaries = JsonSerializer.Deserialize<Dictionary<string, string>[]>(ref reader);
            if (dictionaries is null)
                return Array.Empty<Setting>();

            return dictionaries
                .SelectMany(dictionary => dictionary)
                .Select(keyValuePair => new Setting { Name = keyValuePair.Key, Value = keyValuePair.Value })
                .ToArray();
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyCollection<Setting> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}