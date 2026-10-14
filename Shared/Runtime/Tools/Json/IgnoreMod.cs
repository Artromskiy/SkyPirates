using System.Runtime.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace DVG.SkyPirates.Shared.Tools.Json
{
    public static class IgnoreMod
    {
        public static void Modify(JsonTypeInfo typeInfo)
        {
            foreach (var item in typeInfo.Properties)
            {
                if (!(item.AttributeProvider?.IsDefined(typeof(IgnoreDataMemberAttribute), true) ?? false))
                {
                    continue;
                }

                item.Get = null;
                item.Set = null;
                item.ShouldSerialize = static (_, _) => false;
            }
        }
    }
}
