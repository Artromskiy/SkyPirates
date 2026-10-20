using Delta.ECS;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Components.Framed;

namespace SlyPirates.Shared.Ecs
{
    public static class WorldExtensions
    {
        internal static Query Alive(this Query query) => query.WhereAll<Alive>();

        internal static Query NotDisabled(this Query query) => query.WhereNone<Disabled>();

        internal static ref T GetOrAdd<T>(this World world, Entity entity) where T : struct
        {
            if (!world.Has<T>(entity))
            {
                world.Add<T>(entity);
            }

            return ref world.GetRef<T>(entity);
        }
    }
}
