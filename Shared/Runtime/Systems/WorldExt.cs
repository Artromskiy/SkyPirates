using Delta.ECS;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.Ecs;

namespace DVG.SkyPirates.Shared.Systems
{
    public static class WorldExt
    {
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
