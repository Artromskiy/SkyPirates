using Delta;
using CommunityToolkit.HighPerformance;
using System;
using System.Buffers;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;

namespace DVG.SkyPirates.Shared.Tools.Json
{
    public static class SerializationUTF8
    {
        private static readonly ThreadLocal<Writers> _writers = new(() => new());
        public static readonly JsonSerializerOptions Options;
        public static readonly JsonSerializerOptions OptionsFast;
        static SerializationUTF8()
        {
            Options = new(JsonSerializerDefaults.Strict)
            {
                IncludeFields = true,
                IgnoreReadOnlyFields = false,
                IgnoreReadOnlyProperties = false,
                WriteIndented = true,
                AllowDuplicateProperties = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver().
                WithAddedModifier(IgnoreMod.Modify).
                WithAddedModifier(OrderMod.Modify),
            };

            Options.Converters.Add(new FixConverter());
            Options.Converters.Add(new IdConverterFactory());
            Options.Converters.Add(new NewTypeConverterFactory());
            Options.Converters.Add(new FrozenDictionaryConverterFactory());
            Options.Converters.Add(new ImmutableSortedDictionaryConverterFactory());
            Options.Converters.Add(new ReadOnlyCollectionConverterFactory());
            Options.Converters.Add(new ComponentDataSetConverter());
            Options.Converters.Add(new ComponentIdListConverter());
            Options.Converters.Add(new WorldDataConverter());

            Options.Converters.Add(new VectorConverter<fix, fix2>());
            Options.Converters.Add(new VectorConverter<fix, fix3>());
            Options.Converters.Add(new VectorConverter<fix, fix4>());
            Options.Converters.Add(new VectorConverter<bool, bool2>());
            Options.Converters.Add(new VectorConverter<bool, bool3>());
            Options.Converters.Add(new VectorConverter<bool, bool4>());
            Options.Converters.Add(new VectorConverter<int, int2>());
            Options.Converters.Add(new VectorConverter<int, int3>());
            Options.Converters.Add(new VectorConverter<int, int4>());
            Options.Converters.Add(new VectorConverter<uint, uint2>());
            Options.Converters.Add(new VectorConverter<uint, uint3>());
            Options.Converters.Add(new VectorConverter<uint, uint4>());
            Options.Converters.Add(new VectorConverter<float, float2>());
            Options.Converters.Add(new VectorConverter<float, float3>());
            Options.Converters.Add(new VectorConverter<float, float4>());
            Options.Converters.Add(new VectorConverter<double, double2>());
            Options.Converters.Add(new VectorConverter<double, double3>());
            Options.Converters.Add(new VectorConverter<double, double4>());

            OptionsFast = new(Options)
            {
                WriteIndented = false,
            };
        }

        public static string Serialize<T>(T data)
        {
            var buffer = _writers.Value.Buffer;
            buffer.Clear();
            Serialize(data, buffer);

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        public static T Deserialize<T>(string json)
        {
            var buffer = _writers.Value.Buffer;
            buffer.Clear();
            int count = Encoding.UTF8.GetByteCount(json);
            count = Encoding.UTF8.GetBytes(json, buffer.GetSpan(count));
            buffer.Advance(count);

            return Deserialize<T>(buffer.WrittenMemory);
        }

        public static void Serialize<T>(T data, IBufferWriter<byte> to)
        {
            var writer = _writers.Value.Writer;
            writer.Reset(to);
            JsonSerializer.Serialize(writer, data, Options);
        }

        public static T Deserialize<T>(ReadOnlyMemory<byte> data)
        {
            var reader = new Utf8JsonReader(data.Span);
            return JsonSerializer.Deserialize<T>(ref reader, Options)!;
        }

        public static T DeserializeCompressed<T>(ReadOnlyMemory<byte> data)
        {
            var buffer = _writers.Value.Buffer;
            buffer.Clear();
            Decompress(data, buffer);
            return Deserialize<T>(buffer.WrittenMemory);
        }

        public static void SerializeCompressed<T>(T data, IBufferWriter<byte> to)
        {
            var buffer = _writers.Value.Buffer;
            buffer.Clear();
            Serialize(data, buffer);
            Compress(buffer.WrittenMemory, to);
        }

        public static string SerializeOrdered<T>(T data)
        {
            var buffer = _writers.Value.Buffer;
            var writer = _writers.Value.Writer;
            var document = JsonSerializer.SerializeToDocument(data, Options);
            buffer.Clear();
            writer.Reset(buffer);
            writer.WriteOrdered(document.RootElement);
            writer.Flush();
            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        public static void SerializeOrdered<T>(T data, IBufferWriter<byte> to)
        {
            var writer = _writers.Value.Writer;
            var document = JsonSerializer.SerializeToDocument(data, Options);
            writer.Reset(to);
            writer.WriteOrdered(document.RootElement);
            writer.Flush();
        }

        public static ulong GetHashSum<T>(T data)
        {
            var buffer = _writers.Value.Buffer;
            var writer = _writers.Value.FastWriter;
            buffer.Clear();
            writer.Reset(buffer);
            JsonSerializer.Serialize(writer, data, OptionsFast);
            return JsonHash.GetHash(buffer.WrittenMemory);
        }

        public static void Compress(ReadOnlyMemory<byte> data, IBufferWriter<byte> to)
        {
            using var output = to.AsStream();
            using var input = data.AsStream();
            using DeflateStream dstream = new(output, CompressionLevel.Fastest);
            dstream.Write(data.Span);
        }

        public static void Decompress(ReadOnlyMemory<byte> data, IBufferWriter<byte> to)
        {
            using var input = data.AsStream();
            using var output = to.AsStream();
            using DeflateStream dstream = new(input, CompressionMode.Decompress);
            dstream.CopyTo(output);
        }


        private class Writers
        {
            public readonly Utf8JsonWriter Writer;
            public readonly Utf8JsonWriter FastWriter;
            public readonly ArrayBufferWriter<byte> Buffer;

            public Writers()
            {
                Buffer = new ArrayBufferWriter<byte>();
                Writer = new(Buffer, new JsonWriterOptions() { Indented = true });
                FastWriter = new(Buffer, new JsonWriterOptions() { Indented = false, SkipValidation = true });
            }
        }
    }
}
