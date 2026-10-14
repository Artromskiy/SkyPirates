using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Delta.ECS;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Core;
using SkyPirates.Shared.Data;

namespace SkyPirates.Shared.Tools.Json
{
    public sealed class WorldDataConverter : JsonConverter<WorldData>
    {
        private static readonly Dictionary<string, IWorldDataReader> Readers = CreateReaders();

        public override WorldData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("World data must be an object.");

            var result = new WorldData();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected a component name.");

                string name = reader.GetString()!;
                if (!reader.Read())
                    throw new JsonException($"Missing data for component '{name}'.");
                if (!Readers.TryGetValue(name, out IWorldDataReader componentReader))
                    throw new JsonException($"Unknown component name '{name}'.");

                componentReader.Read(ref reader, options, result);
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, WorldData value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            var components = value.Components;
            var ordered = new List<IWorldDataComponent>(components.Count);
            for (int i = 0; i < components.Count; i++)
                ordered.Add(components[i]);
            ordered.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));

            for (int i = 0; i < ordered.Count; i++)
            {
                IWorldDataComponent component = ordered[i];
                writer.WritePropertyName(component.Name);
                JsonSerializer.Serialize(writer, component.Data, component.DataType, options);
            }

            writer.WriteEndObject();
        }

        private static Dictionary<string, IWorldDataReader> CreateReaders()
        {
            var readers = new Dictionary<string, IWorldDataReader>(StringComparer.Ordinal);
            var action = new RegisterReaders(readers);
            ComponentsRegistry.ForEachData(ref action);
            return readers;
        }

        private interface IWorldDataReader
        {
            void Read(ref Utf8JsonReader reader, JsonSerializerOptions options, WorldData target);
        }

        private sealed class WorldDataReader<T> : IWorldDataReader where T : struct
        {
            public void Read(ref Utf8JsonReader reader, JsonSerializerOptions options, WorldData target)
            {
                Dictionary<int, T> values = JsonSerializer.Deserialize<Dictionary<int, T>>(ref reader, options)!;
                target.Set(values);
            }
        }

        private struct RegisterReaders : IStructGenericAction
        {
            private readonly Dictionary<string, IWorldDataReader> _readers;

            public RegisterReaders(Dictionary<string, IWorldDataReader> readers) => _readers = readers;

            public void Invoke<T>() where T : struct
            {
                _readers.Add(WorldDataComponent<T>.NameForType, new WorldDataReader<T>());
            }
        }
    }
}
