using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace DVG.SkyPirates.Shared.Tools.Json
{
    public static class Utf8JsonWriterExt
    {
        public static void WriteOrdered(this Utf8JsonWriter jsonWriter, JsonElement element)
        {
            if (element.ValueKind is JsonValueKind.Object)
            {
                int count = element.GetPropertyCount();
                int i = count;
                var array = ArrayPool<JsonProperty>.Shared.Rent(count);
                foreach (var item in element.EnumerateObject())
                {
                    array[--i] = item;
                }

                Array.Sort(array, 0, count, JsonPropertyComparer.Default);

                jsonWriter.WriteStartObject();
                for (; i < count; i++)
                {
                    jsonWriter.WritePropertyName(JsonMarshal.GetRawUtf8PropertyName(array[i]));
                    WriteOrdered(jsonWriter, array[i].Value);
                }
                jsonWriter.WriteEndObject();
                ArrayPool<JsonProperty>.Shared.Return(array);
            }

            else if (element.ValueKind is JsonValueKind.Array)
            {
                jsonWriter.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteOrdered(jsonWriter, item);
                }

                jsonWriter.WriteEndArray();
            }

            else
            {
                element.WriteTo(jsonWriter);
            }
        }


        private class JsonPropertyComparer : IComparer<JsonProperty>
        {
            public static JsonPropertyComparer Default { get; } = new();

            public int Compare(JsonProperty x, JsonProperty y)
            {
                var xRaw = JsonMarshal.GetRawUtf8PropertyName(x);
                var yRaw = JsonMarshal.GetRawUtf8PropertyName(y);
                return xRaw.SequenceCompareTo(yRaw);
            }
        }
    }
}
