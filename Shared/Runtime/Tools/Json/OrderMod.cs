using System.Linq;
using System.Text.Json.Serialization.Metadata;

namespace DVG.SkyPirates.Shared.Tools.Json
{
    public static class OrderMod
    {
        public static void Modify(JsonTypeInfo typeInfo)
        {
            var ordered = typeInfo.Properties.
                OrderBy(p => p.Order).
                ThenBy(p => p.Name).
                Select((e, i) => (e, i));

            foreach ((var item, int order) in ordered)
            {
                item.Order = order;
            }
        }
    }
}
