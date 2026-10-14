using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DVG.SkyPirates.Shared.Tools.Json
{
    public sealed class FrozenDictionaryConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
            {
                return false;
            }

            return typeToConvert.GetGenericTypeDefinition() == typeof(FrozenDictionary<,>);
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            var args = type.GetGenericArguments();
            var keyType = args[0];
            var valueType = args[1];

            var converterType = typeof(FrozenDictionaryConverter<,>)
                .MakeGenericType(keyType, valueType);

            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }
    public sealed class FrozenDictionaryConverter<TKey, TValue> :
        JsonConverter<FrozenDictionary<TKey, TValue>>
        where TKey : notnull
    {
        private JsonConverter<TKey>? _keyConverter;
        private JsonConverter<TValue>? _valueConverter;

        public override FrozenDictionary<TKey, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(ref reader, options);

            if (dict == null)
            {
                return null;
            }

            return dict.ToFrozenDictionary();
        }

        public override void Write(Utf8JsonWriter writer, FrozenDictionary<TKey, TValue> dictionary, JsonSerializerOptions options)
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
