using Delta.ECS;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Components.Framed;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Ecs
{
    internal static class WorldExtensions
    {
        public static Query Alive(this Query query) => query.WhereAll<Alive>();

        public static Query NotDisabled(this Query query) => query.WhereNone<Disabled>();

        public static ref T GetOrAdd<T>(this World world, Entity entity) where T : struct
        {
            if (!world.Has<T>(entity))
            {
                world.Add<T>(entity);
            }

            return ref world.GetRef<T>(entity);
        }
    }

    internal sealed class ComponentQueryCache<TQueries> where TQueries : class, new()
    {
        private readonly Dictionary<ComponentId, TQueries> _queriesById = new();

        public TQueries Get(ComponentId componentId)
        {
            if (!_queriesById.TryGetValue(componentId, out var queries))
            {
                _queriesById.Add(componentId, queries = new TQueries());
            }

            return queries;
        }
    }
}
