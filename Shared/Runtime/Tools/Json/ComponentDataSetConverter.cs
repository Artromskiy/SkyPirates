using Delta.ECS;
using SlyPirates.Shared.Data;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlyPirates.Shared.Tools.Json
{
    public sealed class ComponentDataSetConverter : JsonConverter<ComponentDataSet>
    {
        private readonly World? _world;

        public ComponentDataSetConverter() { }

        public ComponentDataSetConverter(World world) => _world = world;

        public override ComponentDataSet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Component data must be an object.");
            }

            if (_world == null)
            {
                throw new JsonException("Deserializing component data requires a registered world.");
            }

            var result = new ComponentDataSet();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected a component name.");
                }

                var name = reader.GetString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new JsonException("Component name cannot be empty.");
                }

                if (!reader.Read())
                {
                    throw new JsonException($"Missing value for component '{name}'.");
                }

                using JsonDocument value = JsonDocument.ParseValue(ref reader);
                ComponentId componentId = _world.GetAlias(name);
                var visitor = new ReadComponentData(name, value.RootElement, options);
                _world.Components.Visit(componentId, visitor);
                result.Add(
                    _world,
                    componentId,
                    visitor.Entry ?? throw new JsonException($"Component alias '{name}' is not a value component."));
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

        private sealed class ReadComponentData : IStructComponentVisitor
        {
            private readonly string _name;
            private readonly JsonElement _value;
            private readonly JsonSerializerOptions _options;

            public ComponentDataEntry? Entry { get; private set; }

            public ReadComponentData(string name, JsonElement value, JsonSerializerOptions options)
            {
                _name = name;
                _value = value;
                _options = options;
            }

            public void Visit<T>(ComponentId componentId) where T : struct
            {
                T value = _value.Deserialize<T>(_options);
                Entry = new ComponentDataEntry<T>(_name, in value);
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

            public void Write<T>(string name, in T value) where T : struct
                => JsonSerializer.Serialize(_writer, value, _options);
        }
    }
}
