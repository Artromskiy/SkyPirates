using DVG.Editor.CodeGen;
using System;
using System.Collections.Generic;
using UnityCodeGen;
using UnityEditor;

namespace DVG.Json.Editor
{
    [Generator]
    public class ScriptableWrapperGenerator : BaseGenerator
    {
        protected override CodePath CodePath { get; } = CodePath.Editor;

        protected override void Generate(Context ctx)
        {
            TypeCache.TypeCollection types = TypeCache.GetTypesWithAttribute<JsonAssetAttribute>();
            ctx.AddCode($"JsonWrappers", Template(types));
        }

        private string Template(IEnumerable<Type> types) =>
$@"
using {typeof(JsonWrapper<>).Namespace};

{types.Loop(type => $@"
namespace {type.Namespace}
{{
    public class JsonWrapper{type.Name} : JsonWrapper<{type.Name}>{{ }}
}}

")}
";
    }
}
