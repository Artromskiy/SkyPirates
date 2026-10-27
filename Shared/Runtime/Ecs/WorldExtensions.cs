using Delta.ECS;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Components.Framed;

namespace SlyPirates.Shared.Ecs
{
    public static class WorldExtensions
    {
        internal static Query Alive(this Query query) => query.WhereAll<Alive>();

        internal static Query NotDisabled(this Query query) => query.WhereNone<Disabled>();
    }
}
