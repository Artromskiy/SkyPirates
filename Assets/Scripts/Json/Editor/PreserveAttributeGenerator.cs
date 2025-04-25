using DVG.Editor.CodeGen;
using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace DVG.Json.Editor
{
    public abstract class PreserveAttributeGenerator : BaseGenerator
    {
        private string TypeToBase(Type type) => type.IsClass ? "class" : "struct";
        protected string Template(IEnumerable<Type> types) =>
$@"
using {typeof(PreserveAttribute).Namespace};

{types.Loop(type => $@"
namespace {type.Namespace}
{{
    [Preserve]
    partial {TypeToBase(type)} {type.Name} {{ }}
}}

")}
";
    }
}
