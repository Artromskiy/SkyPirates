using Delta.ECS;
using Delta.ECS.Integration;
using SlyPirates.Shared.Data;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlyPirates.Shared.Tools.Json
{
    public sealed class EntityDataJsonConverter : JsonConverter<EntityData>
    {
        private readonly World? _world;

        public EntityDataJsonConverter() { }

        public EntityDataJsonConverter(World world) => _world = world;

        public override EntityData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Entity data must be an object.");
            }

            World world = _world ?? throw new JsonException("Deserializing entity data requires a registered world.");
            ReadOnlyMemory<ComponentDescriptor> components = ((IEcsWorld)world).Catalog.Components;
            var result = new EntityData();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected a component name.");
                }

                string? name = reader.GetString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new JsonException("Component name cannot be empty.");
                }

                if (!reader.Read())
                {
                    throw new JsonException($"Missing value for component '{name}'.");
                }

                using JsonDocument value = JsonDocument.ParseValue(ref reader);
                ComponentId componentId = world.GetAlias(name);
                if (!world.Components.TryVisit(componentId, new ReadComponentData(components, result, value.RootElement, options)))
                {
                    throw new JsonException($"Component alias '{name}' is not a serializable value component or tag.");
                }

                if (!result.Has(componentId))
                {
                    throw new JsonException($"Component alias '{name}' is not a value component or tag.");
                }
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, EntityData value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            if (value.Count != 0)
            {
                World world = _world ?? throw new JsonException("Serializing entity data requires a registered world.");
                ReadOnlyMemory<ComponentDescriptor> components = ((IEcsWorld)world).Catalog.Components;
                var visitor = new WriteComponentData(components, writer, value, options);
                foreach (ComponentId componentId in value.ComponentIds)
                {
                    if (!world.Components.TryVisit(componentId, visitor))
                    {
                        throw new JsonException($"Component id '{componentId.Value}' is not a serializable value component or tag.");
                    }
                }
            }
            writer.WriteEndObject();
        }

        private static bool IsTag(ReadOnlyMemory<ComponentDescriptor> components, ComponentId componentId)
            => components.Span[componentId.Value].IsTag;

        private readonly struct ReadComponentData : IStructComponentVisitor
        {
            private readonly ReadOnlyMemory<ComponentDescriptor> _components;
            private readonly EntityData _data;
            private readonly JsonElement _value;
            private readonly JsonSerializerOptions _options;

            public ReadComponentData(ReadOnlyMemory<ComponentDescriptor> components, EntityData data, JsonElement value, JsonSerializerOptions options)
            {
                _components = components;
                _data = data;
                _value = value;
                _options = options;
            }

            public void Visit<T>(ComponentId componentId) where T : struct
            {
                if (IsTag(_components, componentId))
                {
                    _data.AddTag(componentId);
                    return;
                }

                T value = _value.Deserialize<T>(_options);
                _data.AddOrSet(componentId, in value);
            }
        }

        private readonly struct WriteComponentData : IStructComponentVisitor
        {
            private readonly ReadOnlyMemory<ComponentDescriptor> _components;
            private readonly Utf8JsonWriter _writer;
            private readonly EntityData _data;
            private readonly JsonSerializerOptions _options;

            public WriteComponentData(ReadOnlyMemory<ComponentDescriptor> components, Utf8JsonWriter writer, EntityData data, JsonSerializerOptions options)
            {
                _components = components;
                _writer = writer;
                _data = data;
                _options = options;
            }

            public void Visit<T>(ComponentId componentId) where T : struct
            {
                _writer.WritePropertyName(EntityDataComponentMetadata<T>.Alias);
                if (IsTag(_components, componentId))
                {
                    _writer.WriteStartObject();
                    _writer.WriteEndObject();
                    return;
                }

                T value = _data.Get<T>(componentId);
                JsonSerializer.Serialize(_writer, value, _options);
            }
        }
    }
}
