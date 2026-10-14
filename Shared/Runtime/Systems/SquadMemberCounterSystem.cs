using Delta;
using Delta.ECS;
using SkyPirates.Shared.Ecs;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Core.Collections;
using SkyPirates.Shared.Components.Framed;
using SkyPirates.Shared.IServices.TickableExecutors;

namespace SkyPirates.Shared.Systems
{
    public class SquadMemberCounterSystem : IDeltaTickableExecutor
    {
        private Query? _unitsDescCache;
        private Query _unitsDesc => _unitsDescCache ??= _world.
            WhereAll<SquadMember>().Alive().NotDisabled();

        private Query? _squadsDescCache;
        private Query _squadsDesc => _squadsDescCache ??= _world.
            WhereAll<Squad, SyncId, SquadMemberCount>().Alive().NotDisabled();

        private readonly World _world;

        private readonly Lookup<int> _unitCountPerSquad = new();

        public SquadMemberCounterSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            _unitCountPerSquad.Clear();

            var unitCounts = _unitCountPerSquad;
            var unitsDesc = _unitsDesc;
            _world.ForEach<Lookup<int>, SquadMember>(in unitsDesc, ref unitCounts,
                static (ref Lookup<int> counts, in SquadMember member) =>
                {
                    if (!counts.ContainsKey(member.SquadId))
                    {
                        counts[member.SquadId] = 0;
                    }

                    counts[member.SquadId]++;
                }).Invoke(ref unitCounts);

            var counts = _unitCountPerSquad;
            var squadsDesc = _squadsDesc;
            _world.ForEach<Lookup<int>, SyncId, SquadMemberCount>(in squadsDesc, ref counts,
                static (ref Lookup<int> unitCounts, in SyncId syncId, ref SquadMemberCount memberCount) =>
                {
                    unitCounts.TryGetValue(syncId.Value, out int count);
                    memberCount = count;
                }).Invoke(ref counts);
        }
    }
}
