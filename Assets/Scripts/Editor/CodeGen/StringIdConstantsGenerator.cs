using DVG.Editor.Configs;
using System;
using System.Collections.Generic;
using UnityCodeGen;
using UnityEditor;

namespace DVG.Editor.CodeGen
{
    [Generator]
    public class StringIdConstantsGenerator : BaseGenerator
    {
        protected override CodePath CodePath => CodePath.Runtime;

        protected override void Generate(Context ctx)
        {
            var stringIdConfigTypes = TypeCache.GetTypesDerivedFrom(typeof(StringIdEditorConfig<,>));
            foreach (var item in stringIdConfigTypes)
            {
                var instance = item.BaseType.BaseType.GetProperty("Instance").GetValue(null);
                if (instance == null)
                    continue;
                var values = (instance as IStringIdEditorConfig).Values;
                if (values == null)
                    continue;
                var idType = item.BaseType.GenericTypeArguments[0];
                ctx.AddCode($"{idType.Name}.Constants.cs", Template(idType, values));
            }
        }

        private string Template(Type type, IEnumerable<string> values) =>
$@"
namespace {type.Namespace}
{{
    partial struct {type.Name}
    {{
        public static class Constants
        {{
{values.Loop(s => $@"
            public static readonly {type.Name} {s} = new (""{s}"");")}
        }}
    }}
}}
";
    }
}