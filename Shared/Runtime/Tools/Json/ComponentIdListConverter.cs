using SkyPirates.Shared.Data;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkyPirates.Shared.Tools.Json
{
    public sealed class ComponentIdListConverter : JsonConverter<ComponentIdList>
    {
        public override ComponentIdList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Component list must be an object.");

            var result = new ComponentIdList();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected a component name.");
                var name = reader.GetString();
                if (string.IsNullOrWhiteSpace(name))
                    throw new JsonException("Component name cannot be empty.");
                if (!reader.Read() || (reader.TokenType != JsonTokenType.True && reader.TokenType != JsonTokenType.False))
                    throw new JsonException("Component list values must be booleans.");
                if (reader.TokenType == JsonTokenType.True)
                    result.Add(name);
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, ComponentIdList value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var item in value)
                writer.WriteBoolean(item, true);
            writer.WriteEndObject();
        }
    }
}
