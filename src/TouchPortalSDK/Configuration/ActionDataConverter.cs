using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using TouchPortalSDK.Messages.Models;

namespace TouchPortalSDK.Configuration
{
    internal class ActionDataConverter : JsonConverter<ActionData>
    {
        public override ActionData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var ret = new ActionData();
            var actionDataDicts = JsonSerializer.Deserialize<ActionData[]>(ref reader);
            if (actionDataDicts != null){
                foreach (var dict in actionDataDicts) {
                    string id = dict.GetValueOrDefault("id", string.Empty);
                    if (TouchPortalOptions.ActionDataIdSeparator != '\0')
                        id = id.Split(TouchPortalOptions.ActionDataIdSeparator, StringSplitOptions.RemoveEmptyEntries)[^1];
                    ret.TryAdd(id, dict.GetValueOrDefault("value", string.Empty));
                }
            }
            return ret;
        }

        public override void Write(Utf8JsonWriter writer, ActionData value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
