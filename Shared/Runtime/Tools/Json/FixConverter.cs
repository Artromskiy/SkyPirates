using Delta;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DVG.SkyPirates.Shared.Tools.Json
{
    public class FixConverter : JsonConverter<fix>
    {
        public override fix Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return (fix)reader.GetDecimal();
        }

        public override void Write(Utf8JsonWriter writer, fix value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((decimal)value);
        }
    }
}
