using Delta.ECS;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Data;

namespace DVG.SkyPirates.Shared.Ecs
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

        public static void SetEntityData(this World world, Entity entity, ComponentDataSet components)
        {
            foreach (var entry in components)
            {
                entry.Visit(world, new ApplyEntityData(world, entity));
            }
        }

        private readonly struct ApplyEntityData : IStructDataVisitor
        {
            private readonly Entity _entity;
            private readonly World _world;

            public ApplyEntityData(World world, Entity entity)
            {
                _entity = entity;
                _world = world;
            }

            public void Visit<T>(ComponentId componentId, in T value) where T : struct
            {
                if (!_world.Has(_entity, componentId))
                {
                    _world.Add(_entity, componentId);
                }

                _world.GetRef<T>(_entity, componentId) = value;
            }
        }
    }
}
