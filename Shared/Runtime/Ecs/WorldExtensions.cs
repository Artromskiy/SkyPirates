using Delta.ECS;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Data;
using System;

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

        public static void SetEntityData(this World world, Entity entity, ComponentDataSet components)
        {
            ComponentId[] componentIds = components.GetComponentIds(world);
            var missing = new ComponentId[componentIds.Length];
            int missingCount = 0;
            for (int i = 0; i < componentIds.Length; i++)
            {
                if (!world.Has(entity, componentIds[i]))
                {
                    missing[missingCount++] = componentIds[i];
                }
            }

            if (missingCount != 0)
            {
                world.Add(entity, missing.AsSpan(0, missingCount));
            }

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

            public void Visit<T>(ComponentId componentId, in T value) where T : struct =>
                _world.GetRef<T>(_entity, componentId) = value;
        }
    }
}
