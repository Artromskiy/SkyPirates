using System;
using System.Collections.Generic;
using Delta.ECS;
using SkyPirates.Shared.Data;

namespace SkyPirates.Shared.Ecs
{
    public interface IWorldHistory : IDisposable
    {
        void Save(int tick);
        void SaveBaseline();
        void GoTo(int tick);
        void Rollback(int tick);
        WorldData GetSnapshot(int tick);
        void ApplySnapshot(WorldData snapshot, IReadOnlyDictionary<int, Entity> entitiesBySyncId);
        void RetireEntityFromSnapshot(Entity entity);
        void MarkForDisposal(Entity entity, int tick);
        void CollectExpiredEntities(int tick, List<Entity> entities);
        void DisposeEntity(Entity entity);
        void Clear();
    }
}
