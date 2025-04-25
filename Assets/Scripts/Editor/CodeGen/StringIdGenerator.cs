using DVG.Core.Ids.Attributes;
using System;
using UnityCodeGen;
using UnityEditor;

namespace DVG.Editor.CodeGen
{
    [Generator]
    public class StringIdGenerator : BaseGenerator
    {
        protected override CodePath CodePath => CodePath.Runtime;
        protected override void Generate(Context ctx)
        {
            var idTypes = TypeCache.GetTypesWithAttribute<StringIdAttribute>();
            foreach (var type in idTypes)
                ctx.AddCode($"{type.Name}", Template(type));
        }

        private string Template(Type type) =>
$@"
using DVG.Core.Ids;
using DVG.Core.Ids.Utilities;
using System.ComponentModel;
using System;
using Newtonsoft.Json;
using UnityEngine;

namespace {type.Namespace}
{{
    [TypeConverter(typeof(IdTypeConverter))]
    [JsonConverter(typeof(IdJsonConverter))]
    [Serializable]
    partial struct {type.Name} : IStringId, IEquatable<{type.Name}>, IComparable<{type.Name}>
    {{
        [field: SerializeField]
        public string Value {{ get; private set; }}
        private const string NoneValue = ""None"";
        public static readonly {type.Name} None = new(NoneValue);

        public {type.Name}(string value)
        {{
            Value = value;
        }}

        public readonly bool IsNone => string.IsNullOrEmpty(Value) || Value == NoneValue;
        public readonly bool Equals({type.Name} other) => Value == other.Value;
        public readonly int CompareTo({type.Name} other) => Equals(other) ? 0 : string.Compare(Value, other.Value);
        public override readonly bool Equals(object obj) => obj is {type.Name} other && Equals(other);
        public override readonly string ToString() => Value;
        public override readonly int GetHashCode() => IsNone ? 0 : Value.GetHashCode();
        public static bool operator ==({type.Name} a, {type.Name} b) => a.Value == b.Value || (a.IsNone && b.IsNone);
        public static bool operator !=({type.Name} a, {type.Name} b) => !(a == b);

        private class IdTypeConverter : IdTypeConverter<{type.Name}>
        {{
            protected override {type.Name} CreateFromSource(string srcData) => new(srcData);
        }}

        private class IdJsonConverter : IdJsonConverter<{type.Name}>
        {{
            protected override {type.Name} CreateFromRawData(string type) => new(type);
        }}
    }}
}}
";
    }
}