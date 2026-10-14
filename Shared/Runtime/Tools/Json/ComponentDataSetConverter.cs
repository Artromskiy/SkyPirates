using SkyPirates.Shared.Core;
using Delta.ECS;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.Components.Framed;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkyPirates.Shared.Tools.Json
{
    public sealed class ComponentDataSetConverter : JsonConverter<ComponentDataSet>
    {
        private static readonly ComponentDataTypeRegistry Types = ComponentDataTypeRegistry.Create();

        public override ComponentDataSet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Component data must be an object.");

            var result = new ComponentDataSet();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected a component name.");

                var name = reader.GetString();
                if (string.IsNullOrWhiteSpace(name))
                    throw new JsonException("Component name cannot be empty.");
                if (!reader.Read())
                    throw new JsonException($"Missing value for component '{name}'.");

                result.Add(Types.Read(name, ref reader, options));
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, ComponentDataSet value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            var visitor = new WriteComponentVisitor(writer, options);
            foreach (var entry in value)
            {
                writer.WritePropertyName(entry.Name);
                entry.Write(visitor);
            }
            writer.WriteEndObject();
        }

        private sealed class ComponentDataTypeRegistry
        {
            private readonly Dictionary<string, IComponentDataTypeReader> _readers = new(StringComparer.Ordinal);

            public static ComponentDataTypeRegistry Create()
            {
                var registry = new ComponentDataTypeRegistry();
                var action = new RegisterTypes(registry);
                ComponentsRegistry.ForEachData(ref action);
                registry.Register<Disabled>();
                registry.Register<Temp>();
                return registry;
            }

            public ComponentDataEntry Read(string name, ref Utf8JsonReader reader, JsonSerializerOptions options)
            {
                if (!_readers.TryGetValue(name, out var typeReader))
                    throw new JsonException($"Unknown component name '{name}'.");
                return typeReader.Read(name, ref reader, options);
            }

            private void Register<T>() where T : struct
            {
                var name = typeof(T).Name;
                if (_readers.ContainsKey(name))
                    throw new InvalidOperationException($"Duplicate component name '{name}'.");
                _readers.Add(name, new ComponentDataTypeReader<T>());
            }

            private struct RegisterTypes : IStructGenericAction
            {
                private readonly ComponentDataTypeRegistry _registry;

                public RegisterTypes(ComponentDataTypeRegistry registry) => _registry = registry;
                public void Invoke<T>() where T : struct => _registry.Register<T>();
            }
        }

        private interface IComponentDataTypeReader
        {
            ComponentDataEntry Read(string name, ref Utf8JsonReader reader, JsonSerializerOptions options);
        }

        private sealed class ComponentDataTypeReader<T> : IComponentDataTypeReader where T : struct
        {
            public ComponentDataEntry Read(string name, ref Utf8JsonReader reader, JsonSerializerOptions options)
            {
                var value = JsonSerializer.Deserialize<T>(ref reader, options);
                return new ComponentDataEntry<T>(name, in value);
            }
        }

        private readonly struct WriteComponentVisitor : IComponentDataWriter
        {
            private readonly Utf8JsonWriter _writer;
            private readonly JsonSerializerOptions _options;

            public WriteComponentVisitor(Utf8JsonWriter writer, JsonSerializerOptions options)
            {
                _writer = writer;
                _options = options;
            }

            public void Write<T>(string name, in T value)
                => JsonSerializer.Serialize(_writer, value, _options);
        }
    }
}
