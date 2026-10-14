using DVG.Ids;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DVG.SkyPirates.Shared.Tools.Json
{
    public class IdConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.IsValueType &&
            typeof(IId).IsAssignableFrom(typeToConvert);

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            var converterType = typeof(IdConverter<>).MakeGenericType(type);
            return (JsonConverter)Activator.CreateInstance(converterType);
        }
    }

    public sealed class IdConverter<T> : JsonConverter<T>
        where T : struct, IId
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? parameter = reader.GetString();
            parameter = string.IsNullOrWhiteSpace(parameter) ? "None" : parameter;
            return (T)Activator.CreateInstance(typeof(T), parameter);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.Value, options);
        }

        public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Read(ref reader, typeToConvert, options);
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WritePropertyName(value.Value);
        }
    }
}
