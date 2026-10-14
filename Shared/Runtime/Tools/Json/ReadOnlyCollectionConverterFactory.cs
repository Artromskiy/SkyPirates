using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DVG.SkyPirates.Shared.Tools.Json
{
    public sealed class ReadOnlyCollectionConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
            {
                return false;
            }

            var definition = typeToConvert.GetGenericTypeDefinition();
            return definition == typeof(IReadOnlyList<>)
                || definition == typeof(IReadOnlyCollection<>)
                || definition == typeof(IReadOnlyDictionary<,>);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var definition = typeToConvert.GetGenericTypeDefinition();
            var arguments = typeToConvert.GetGenericArguments();
            var converterDefinition = definition == typeof(IReadOnlyList<>)
                ? typeof(ReadOnlyListConverter<>).MakeGenericType(arguments)
                : definition == typeof(IReadOnlyCollection<>)
                    ? typeof(ReadOnlyCollectionConverter<>).MakeGenericType(arguments)
                    : typeof(ReadOnlyDictionaryConverter<,>).MakeGenericType(arguments);
            return (JsonConverter)Activator.CreateInstance(converterDefinition, nonPublic: true)!;
        }

        private sealed class ReadOnlyListConverter<T> : JsonConverter<IReadOnlyList<T>>
        {
            public override IReadOnlyList<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var values = JsonSerializer.Deserialize<List<T>>(ref reader, options);
                return values == null ? null! : values.AsReadOnly();
            }

            public override void Write(Utf8JsonWriter writer, IReadOnlyList<T> value, JsonSerializerOptions options) =>
                JsonSerializer.Serialize<IEnumerable<T>>(writer, value, options);
        }

        private sealed class ReadOnlyCollectionConverter<T> : JsonConverter<IReadOnlyCollection<T>>
        {
            public override IReadOnlyCollection<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var values = JsonSerializer.Deserialize<List<T>>(ref reader, options);
                return values == null ? null! : new ReadOnlyCollection<T>(values);
            }

            public override void Write(Utf8JsonWriter writer, IReadOnlyCollection<T> value, JsonSerializerOptions options) =>
                JsonSerializer.Serialize<IEnumerable<T>>(writer, value, options);
        }

        private sealed class ReadOnlyDictionaryConverter<TKey, TValue> : JsonConverter<IReadOnlyDictionary<TKey, TValue>>
            where TKey : notnull
        {
            public override IReadOnlyDictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var values = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(ref reader, options);
                return values == null ? null! : new ReadOnlyDictionary<TKey, TValue>(values);
            }

            public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<TKey, TValue> value, JsonSerializerOptions options) =>
                JsonSerializer.Serialize(writer, value.ToDictionary(pair => pair.Key, pair => pair.Value), options);
        }
    }
}
