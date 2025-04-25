using DVG.Core.Ids;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DVG.Json.Editor
{
    internal class DebugConfigSerializerSettings : JsonSerializerSettings
    {
        private static DebugConfigSerializerSettings _instance;
        public static DebugConfigSerializerSettings Instance => _instance ??= new DebugConfigSerializerSettings();
        private DebugConfigSerializerSettings()
        {
            DefaultValueHandling = DefaultValueHandling.Ignore;
            NullValueHandling = NullValueHandling.Ignore;
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            ContractResolver = new DebugSerializeContractResolver();
            Converters.Add(new CustomEnumConverter() { AllowIntegerValues = false});
        }

        private class CustomEnumConverter: StringEnumConverter
        {
            private readonly HashSet<int> _enumsSet = new();
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                Type enumType = value.GetType();
                Enum castedValue = (Enum)value;

                if (castedValue.CompareTo(Enum.ToObject(enumType, -1)) != 0)
                {
                    base.WriteJson(writer, value, serializer);
                    return;
                }

                _enumsSet.Clear();
                int newValue = 0;
                var enumValues = (int[])Enum.GetValues(enumType);
                foreach (var enumValue in enumValues)
                    if ((enumValue == 1 || enumValue % 2 == 0) && _enumsSet.Add(enumValue))
                        newValue |= enumValue;
                value = Enum.ToObject(enumType, newValue);

                base.WriteJson(writer, value, serializer);
            }
        }

        private class DebugSerializeContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                if (property.PropertyType == typeof(string))
                    property.ShouldSerialize = ins => !string.IsNullOrEmpty(GetValue(ins, member) as string);
                else if (typeof(IStringId).IsAssignableFrom(property.PropertyType))
                    property.ShouldSerialize = ins => !string.IsNullOrEmpty((GetValue(ins, member) as IStringId).Value);
                else if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                    property.ShouldSerialize = ins => GetValue(ins, member) is IEnumerable enumerable && enumerable.GetEnumerator().MoveNext();

                return property;
            }

            private object GetValue(object ins, MemberInfo member) => member.MemberType switch
            {
                MemberTypes.Property => GetPropertyValue(ins, member.Name),
                MemberTypes.Field => GetFieldValue(ins, member.Name),
                _ => null,
            };

            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            private object GetPropertyValue(object instance, string memberName) => instance.GetType().GetProperty(memberName, bindingFlags)?.GetValue(instance, null);
            private object GetFieldValue(object instance, string memberName) => instance.GetType().GetField(memberName, bindingFlags)?.GetValue(instance);
        }
    }
}