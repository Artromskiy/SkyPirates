using Delta.ECS;
using DVG.SkyPirates.Shared.Components.Runtime;

namespace DVG.SkyPirates.Shared.Ecs
{
    public static class EntitySyncIdExtensions
    {
        public static EntityIdMap GetEntityIdMap(this World world)
        {
            Entity mapEntity = world.WhereAll<EntityIdMap>().First().Invoke();
            return world.Get<EntityIdMap>(mapEntity);
        }

        public static EntityIdMap GetOrCreateEntityIdMap(this World world, int initialCapacity)
        {
            Entity mapEntity = world.WhereAll<EntityIdMap>().First().Invoke();
            if (mapEntity.IsValid)
                return world.Get<EntityIdMap>(mapEntity);

            mapEntity = world.Create<EntityIdMap>();
            var map = new EntityIdMap(initialCapacity);
            world.GetRef<EntityIdMap>(mapEntity) = map;
            return map;
        }

        public static Entity GetEntity(this World world, SyncId syncId)
            => world.GetEntityIdMap().GetEntity(syncId);
    }
}
