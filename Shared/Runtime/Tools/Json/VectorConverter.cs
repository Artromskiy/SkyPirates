using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace DVG.SkyPirates.Shared.Tools.Json
{
    public class VectorConverter<T, V> : JsonConverter<V>
        where V : unmanaged
        where T : unmanaged
    {
        private static readonly ThreadLocal<Writers> _writers = new(() => new());
        private static readonly int _count = (int)typeof(V).GetProperty("Count").GetValue(new V());
        private static readonly byte[] _arrayStart = Encoding.UTF8.GetBytes("[");
        private static readonly byte[] _arrayEnd = Encoding.UTF8.GetBytes("]");
        private static readonly byte[] _comma = Encoding.UTF8.GetBytes(",");

        public override V Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var vector = new V();
            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<V, T>(ref vector), _count);

            for (int i = 0; i < _count; i++)
            {
                reader.Read();
                span[i] = JsonSerializer.Deserialize<T>(ref reader, options);
            }
            reader.Read();
            return vector;
        }

        public override V ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var vector = new V();
            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<V, T>(ref vector), _count);
            var bufferWriter = _writers.Value.BufferWriter;

            bufferWriter.Clear();

            bufferWriter.Write(_arrayStart);
            int written = reader.CopyString(bufferWriter.GetSpan(reader.ValueSpan.Length));
            bufferWriter.Advance(written);
            bufferWriter.Write(_arrayEnd);

            var innerReader = new Utf8JsonReader(bufferWriter.WrittenSpan);
            innerReader.Read();
            for (int i = 0; i < _count; i++)
            {
                innerReader.Read();
                span[i] = JsonSerializer.Deserialize<T>(ref innerReader, options);
            }
            return vector;
        }

        public override void Write(Utf8JsonWriter writer, V vector, JsonSerializerOptions options)
        {
            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<V, T>(ref vector), _count);
            var bufferCache = _writers.Value.BufferCache;
            var bufferWriter = _writers.Value.BufferWriter;
            var jsonWriter = _writers.Value.JsonWriter;

            bufferCache.Clear();
            bufferCache.Write(_arrayStart);
            for (int i = 0; i < _count; i++)
            {
                jsonWriter.Reset();
                bufferWriter.Clear();
                JsonSerializer.Serialize(jsonWriter, span[i], options);
                bufferCache.Write(bufferWriter.WrittenSpan);
                if (i != _count - 1)
                {
                    bufferCache.Write(_comma);
                }
            }
            bufferCache.Write(_arrayEnd);
            writer.WriteRawValue(bufferCache.WrittenSpan);
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, V vector, JsonSerializerOptions options)
        {
            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<V, T>(ref vector), _count);

            var bufferCache = _writers.Value.BufferCache;
            var bufferWriter = _writers.Value.BufferWriter;
            var jsonWriter = _writers.Value.JsonWriter;

            bufferCache.Clear();
            for (int i = 0; i < _count; i++)
            {
                jsonWriter.Reset();
                bufferWriter.Clear();
                JsonSerializer.Serialize(jsonWriter, span[i], options);
                bufferCache.Write(bufferWriter.WrittenSpan);
                if (i != _count - 1)
                {
                    bufferCache.Write(_comma);
                }
            }
            writer.WritePropertyName(bufferCache.WrittenSpan);
        }

        private class Writers
        {
            public readonly Utf8JsonWriter JsonWriter;
            public readonly ArrayBufferWriter<byte> BufferWriter = new();
            public readonly ArrayBufferWriter<byte> BufferCache = new();

            public Writers()
            {
                JsonWriter = new(BufferWriter, new() { Indented = false });
            }
        }
    }
}
