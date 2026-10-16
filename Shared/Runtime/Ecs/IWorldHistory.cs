using System;
using SlyPirates.Shared.Data;

namespace SlyPirates.Shared.Ecs
{
    public interface IWorldHistory : IDisposable
    {
        int OldestTick { get; }
        int NewestTick { get; }
        void Save(int tick);
        void GoTo(int tick);
        void Rollback(int tick);
        WorldData GetSnapshot(int tick);
        void ApplySnapshot(WorldData snapshot, int tick);
        void Clear();
    }
}
