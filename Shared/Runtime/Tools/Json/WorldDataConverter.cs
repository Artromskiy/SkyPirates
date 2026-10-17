using Delta.ECS;
using SlyPirates.Shared.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlyPirates.Shared.Tools.Json
{
    public sealed class WorldDataConverter : JsonConverter<WorldData>
    {
        private static readonly Dictionary<string, Type> TypesByAlias;
        private static readonly Dictionary<Type, string> AliasesByType;

        static WorldDataConverter()
        {
            TypesByAlias = new Dictionary<string, Type>(StringComparer.Ordinal);
            AliasesByType = new Dictionary<Type, string>();

            foreach (IGeneratedComponentRegistration registration in GeneratedComponentCatalog.GetRegistrations())
            {
                Type componentType = registration.ComponentType;
                if (componentType.Assembly != typeof(WorldData).Assembly)
                {
                    continue;
                }

                object[] attributes = componentType.GetCustomAttributes(typeof(ComponentAliasAttribute), false);
                if (attributes.Length == 0)
                {
                    AddAlias(componentType.Name, componentType);
                    continue;
                }

                for (int i = 0; i < attributes.Length; i++)
                {
                    var alias = ((ComponentAliasAttribute)attributes[i]).Alias;
                    AddAlias(alias, componentType);
                    if (!AliasesByType.ContainsKey(componentType))
                    {
                        AliasesByType.Add(componentType, alias);
                    }
                }
            }
        }

        public override WorldData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("World data must be an object.");
            }

            var worldData = new WorldData();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected a component alias.");
                }

                string? alias = reader.GetString();
                if (alias == null || !TypesByAlias.TryGetValue(alias, out Type? componentType))
                {
                    throw new JsonException($"Unknown world data component '{alias}'.");
                }

                if (!reader.Read())
                {
                    throw new JsonException($"Missing data for component '{alias}'.");
                }

                Type componentDataType = typeof(Dictionary<,>).MakeGenericType(typeof(int), componentType);
                object componentData = JsonSerializer.Deserialize(ref reader, componentDataType, options)
                    ?? throw new JsonException($"Component data '{alias}' cannot be null.");
                worldData.Set(componentType, componentData);
            }

            return worldData;
        }

        public override void Write(Utf8JsonWriter writer, WorldData value, JsonSerializerOptions options)
        {
            var components = new List<KeyValuePair<string, object>>();
            foreach (KeyValuePair<Type, object> component in value.Components)
            {
                if (!AliasesByType.TryGetValue(component.Key, out string? alias))
                {
                    throw new JsonException($"World data component '{component.Key.FullName}' has no generated registration.");
                }

                components.Add(new KeyValuePair<string, object>(alias, component.Value));
            }

            components.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Key, right.Key));

            writer.WriteStartObject();
            for (int i = 0; i < components.Count; i++)
            {
                KeyValuePair<string, object> component = components[i];
                writer.WritePropertyName(component.Key);
                JsonSerializer.Serialize(writer, component.Value, component.Value.GetType(), options);
            }
            writer.WriteEndObject();
        }

        private static void AddAlias(string alias, Type componentType)
        {
            if (TypesByAlias.TryGetValue(alias, out Type? existingType) && existingType != componentType)
            {
                throw new InvalidOperationException($"Component alias '{alias}' is assigned to multiple component types.");
            }

            TypesByAlias[alias] = componentType;
        }
    }
}
