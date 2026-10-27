using SlyPirates.Shared.Data;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlyPirates.Shared.Tools.Json
{
    public sealed class ComponentIdListConverter : JsonConverter<ComponentIdList>
    {
        public override ComponentIdList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new ComponentIdList();
            if (reader.TokenType == JsonTokenType.String)
            {
                AddNames(result, reader.GetString());
                return result;
            }

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Component list must be an array of names or a comma-separated name string.");
            }

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType != JsonTokenType.String)
                    throw new JsonException("Component names must be strings.");

                AddNames(result, reader.GetString());
            }

            return result;
        }

        private static void AddNames(ComponentIdList result, string? names)
        {
            if (string.IsNullOrWhiteSpace(names)) return;
            foreach (string name in names.Split(','))
            {
                string trimmed = name.Trim();
                if (trimmed.Length != 0) result.Add(trimmed);
            }
        }

        public override void Write(Utf8JsonWriter writer, ComponentIdList value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                writer.WriteStringValue(item);
            }

            writer.WriteEndArray();
        }
    }
}
