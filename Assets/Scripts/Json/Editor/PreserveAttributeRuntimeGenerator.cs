using DVG.Editor.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityCodeGen;
using UnityEditor;

namespace DVG.Json.Editor
{
    [Generator]
    internal class PreserveAttributeRuntimeGenerator : PreserveAttributeGenerator
    {
        protected override CodePath CodePath { get; } = CodePath.Runtime;

        protected override void Generate(Context ctx)
        {
            IEnumerable<Type> types = TypeCache.GetTypesWithAttribute<JsonAssetAttribute>();
            types = types.Where(type => !type.Namespace.Split('.').Contains("Editor"));
            ctx.AddCode($"JsonPreserve", Template(types));
        }
    }
}
