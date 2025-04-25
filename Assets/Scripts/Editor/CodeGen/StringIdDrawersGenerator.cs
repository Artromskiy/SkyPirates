using DVG.Core.Ids.Attributes;
using DVG.Editor.CustomDrawers;
using System;
using System.Collections.Generic;
using UnityCodeGen;
using UnityEditor;

namespace DVG.Editor.CodeGen
{
    [Generator]
    public class StringIdDrawersGenerator : BaseGenerator
    {
        protected override CodePath CodePath => CodePath.Editor;
        protected override void Generate(Context ctx)
        {
            var drawerTypes = TypeCache.GetTypesWithAttribute<StringIdDrawerAttribute>();
            HashSet<Type> withEditorConfig = new(TypeCache.GetTypesWithAttribute<StringIdEditorConfigAttribute>());

            foreach (var type in drawerTypes)
            {
                var drawerType = ((StringIdDrawerAttribute)Attribute.GetCustomAttribute(type, typeof(StringIdDrawerAttribute))).drawerType;
                if (drawerType == DrawerType.Simple)
                    ctx.AddCode($"{type.Name}Drawer", SimpleDrawerTemplate(type));
                else if (withEditorConfig.Contains(type))
                    ctx.AddCode($"{type.Name}Drawer", PopupDrawerTemplate(type));
            }
        }

        private string SimpleDrawerTemplate(Type type) =>
$@"
using UnityEditor;
using DVG.Editor.Configs;
using {typeof(SimpleStringIdDrawer<>).Namespace};

namespace {type.Namespace}
{{
    [CustomPropertyDrawer(typeof({type.Name}))]
    public class {type.Name}Drawer : SimpleStringIdDrawer<{type.Name}> {{ }}
}}
";

        private string PopupDrawerTemplate(Type type) =>
$@"
using UnityEditor;
using DVG.Editor.Configs;
using {typeof(PopupStringIdDrawer<>).Namespace};

namespace {type.Namespace}
{{
    [CustomPropertyDrawer(typeof({type.Name}))]
    public class {type.Name}Drawer : PopupStringIdDrawer<{type.Name}>
    {{
        protected override string[] GetValues() => {type.Name}EditorConfig.Instance.Values;
    }}
}}
";

    }
}
