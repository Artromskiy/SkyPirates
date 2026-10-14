using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;

namespace SkyPirates.Shared.Data
{
    public struct EntityParameters
    {
        public SyncId SyncId;
        public SyncIdReserve SyncIdReserve;
        public RandomSeed RandomSeed;

        public EntityParameters(SyncId syncId, SyncIdReserve syncIdReserve, RandomSeed randomSeed)
        {
            SyncId = syncId;
            SyncIdReserve = syncIdReserve;
            RandomSeed = randomSeed;
        }
    }
}
