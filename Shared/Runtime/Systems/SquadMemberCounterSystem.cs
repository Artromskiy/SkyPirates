using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Core.Collections;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
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
                    if (!counts.ContainsKey(member.SquadId.Value))
                    {
                        counts[member.SquadId.Value] = 0;
                    }

                    counts[member.SquadId.Value]++;
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
