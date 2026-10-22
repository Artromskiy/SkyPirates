using Delta.ECS;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlyPirates.Shared.Tools.Json
{
    public sealed class EntityDataJsonConverter : JsonConverter<EntityData>
    {
        private readonly World? _world;

        public EntityDataJsonConverter() { }

        public EntityDataJsonConverter(World world)
        {
            _world = world;
        }

        public override EntityData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Entity data must be an object.");
            }

            World world = _world ?? throw new JsonException("Deserializing entity data requires a registered world.");
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
                if (!world.Components.TryVisit(componentId, new ReadComponentData(result, value.RootElement, options)))
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
                var visitor = new WriteComponentData(world, writer, value, options);
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

        private static string GetAlias(World world, ComponentId componentId)
        {
            ReadOnlySpan<string> aliases = world.Components.GetAliases(componentId);
            return aliases.IsEmpty ? world.Components.GetComponentType(componentId).Name : aliases[0];
        }

        private readonly struct ReadComponentData : IStructComponentVisitor
        {
            private readonly EntityData _data;
            private readonly JsonElement _value;
            private readonly JsonSerializerOptions _options;

            public ReadComponentData(EntityData data, JsonElement value, JsonSerializerOptions options)
            {
                _data = data;
                _value = value;
                _options = options;
            }

            public void Visit<T>(ComponentId componentId) where T : struct
            {
                T value = _value.Deserialize<T>(_options);
                _data.AddOrSet(componentId, in value);
            }
        }

        private readonly struct WriteComponentData : IStructComponentVisitor
        {
            private readonly World _world;
            private readonly Utf8JsonWriter _writer;
            private readonly EntityData _data;
            private readonly JsonSerializerOptions _options;

            public WriteComponentData(World world, Utf8JsonWriter writer, EntityData data, JsonSerializerOptions options)
            {
                _world = world;
                _writer = writer;
                _data = data;
                _options = options;
            }

            public void Visit<T>(ComponentId componentId) where T : struct
            {
                _writer.WritePropertyName(GetAlias(_world, componentId));
                T value = _data.Get<T>(componentId);
                JsonSerializer.Serialize(_writer, value, _options);
            }
        }
    }
}
