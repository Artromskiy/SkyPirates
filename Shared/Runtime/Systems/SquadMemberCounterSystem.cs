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
        private Lookup<int> _countState;
        private readonly IOperation<Lookup<int>> _countUnits;
        private readonly IOperation<Lookup<int>> _writeCounts;

        public SquadMemberCounterSystem(World world)
        {
            _world = world;
            _countState = _unitCountPerSquad;
            var unitsDesc = _unitsDesc;
            _countUnits = world.ForEach<Lookup<int>, SquadMember>(in unitsDesc, ref _countState,
                static (ref Lookup<int> counts, in SquadMember member) =>
                {
                    counts.TryGetValue(member.SquadId.Value, out int count);
                    counts[member.SquadId.Value] = count + 1;
                });

            var squadsDesc = _squadsDesc;
            _writeCounts = world.ForEach<Lookup<int>, SyncId, SquadMemberCount>(in squadsDesc, ref _countState,
                static (ref Lookup<int> unitCounts, in SyncId syncId, ref SquadMemberCount memberCount) =>
                {
                    unitCounts.TryGetValue(syncId.Value, out int count);
                    memberCount = count;
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _unitCountPerSquad.Clear();

            _countUnits.Invoke(ref _countState);
            _writeCounts.Invoke(ref _countState);
        }
    }
}
