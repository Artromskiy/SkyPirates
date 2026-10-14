using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DVG.SkyPirates.Shared.Tools.Json
{
    public sealed class ImmutableSortedDictionaryConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
            {
                return false;
            }

            return typeToConvert.GetGenericTypeDefinition() == typeof(ImmutableSortedDictionary<,>);
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            var args = type.GetGenericArguments();
            var keyType = args[0];
            var valueType = args[1];

            var converterType = typeof(ImmutableSortedDictionaryConverter<,>)
                .MakeGenericType(keyType, valueType);

            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }

    public sealed class ImmutableSortedDictionaryConverter<TKey, TValue> :
        JsonConverter<ImmutableSortedDictionary<TKey, TValue>>
        where TKey : notnull
    {
        private JsonConverter<TKey>? _keyConverter;
        private JsonConverter<TValue>? _valueConverter;

        // TODO deserialize using ImmutableSortedDictionary.Builder
        public override ImmutableSortedDictionary<TKey, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(ref reader, options);

            if (dict == null)
            {
                return null;
            }

            return dict.ToImmutableSortedDictionary();
        }

        public override void Write(Utf8JsonWriter writer, ImmutableSortedDictionary<TKey, TValue> dictionary, JsonSerializerOptions options)
        {
            _keyConverter ??= (options.GetConverter(typeof(TKey)) as JsonConverter<TKey>)!;
            _valueConverter ??= (options.GetConverter(typeof(TValue)) as JsonConverter<TValue>)!;

            writer.WriteStartObject();
            foreach (var (key, value) in dictionary)
            {
                _keyConverter.WriteAsPropertyName(writer, key, options);
                _valueConverter.Write(writer, value, options);
            }
            writer.WriteEndObject();

        }
    }
}
