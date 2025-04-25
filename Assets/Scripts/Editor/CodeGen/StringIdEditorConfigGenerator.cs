using DVG.Core.Ids.Attributes;
using DVG.Editor.Configs;
using System;
using UnityCodeGen;
using UnityEditor;

namespace DVG.Editor.CodeGen
{
    [Generator]
    public class StringIdEditorConfigGenerator : BaseGenerator
    {
        protected override CodePath CodePath => CodePath.Editor;
        protected override void Generate(Context ctx)
        {
            var idTypes = TypeCache.GetTypesWithAttribute<StringIdEditorConfigAttribute>();
            foreach (var type in idTypes)
                ctx.AddCode($"{type.Name}EditorConfig", Template(type));
        }

        private string Template(Type type) =>
$@"
using {type.Namespace};
using UnityEngine;

namespace {typeof(StringIdEditorConfig<,>).Namespace}
{{
    [CreateAssetMenu(fileName = nameof({type.Name}EditorConfig), menuName = ""EditorConfigs/"" + nameof({type.Name}EditorConfig))]
    public class {type.Name}EditorConfig : StringIdEditorConfig<{type.Name}, {type.Name}EditorConfig> {{ }}
}}
";
    }
}