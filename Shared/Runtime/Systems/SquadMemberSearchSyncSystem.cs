using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Core.Collections;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
{
    public class SquadMemberSearchSyncSystem : IDeltaTickableExecutor
    {
        private Query? _unitsDescCache;
        private Query _unitsDesc => _unitsDescCache ??= _world.
            WhereAll<SquadMember, TargetSearchDistance, TargetSearchPosition>().
            Alive().NotDisabled();

        private Query? _squadsDescCache;
        private Query _squadsDesc => _squadsDescCache ??= _world.
            WhereAll<Squad, SyncId, TargetSearchDistance, TargetSearchPosition, Fixation>().
            Alive().NotDisabled();

        private readonly World _world;

        private readonly Lookup<TargetSearchData> _searchDataPerSquad = new();
        private Lookup<TargetSearchData> _searchDataState;
        private readonly IOperation<Lookup<TargetSearchData>> _collectSquadData;
        private readonly IOperation<Lookup<TargetSearchData>> _syncMembers;

        public SquadMemberSearchSyncSystem(World world)
        {
            _world = world;
            _searchDataState = _searchDataPerSquad;
            var squadsDesc = _squadsDesc;
            _collectSquadData = world.ForEach<Lookup<TargetSearchData>, SyncId, TargetSearchPosition, TargetSearchDistance, Fixation>(in squadsDesc, ref _searchDataState,
                static (ref Lookup<TargetSearchData> dataPerSquad, in SyncId syncId, in TargetSearchPosition searchPosition, in TargetSearchDistance searchDistance, in Fixation fixation) =>
                {
                    TargetSearchDistance distance = fixation ? fix.Zero : searchDistance;
                    dataPerSquad[syncId.Value] = new(searchPosition, distance);
                });

            var unitsDesc = _unitsDesc;
            _syncMembers = world.ForEach<Lookup<TargetSearchData>, SquadMember, TargetSearchPosition, TargetSearchDistance>(in unitsDesc, ref _searchDataState,
                static (ref Lookup<TargetSearchData> dataPerSquad, in SquadMember squadMember, ref TargetSearchPosition searchPosition, ref TargetSearchDistance searchDistance) =>
                {
                    dataPerSquad.TryGetValue(squadMember.SquadId.Value, out var data);
                    searchPosition = data.TargetSearchPosition;
                    searchDistance = data.TargetSearchDistance;
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _searchDataPerSquad.Clear();

            _collectSquadData.Invoke(ref _searchDataState);
            _syncMembers.Invoke(ref _searchDataState);
        }

        internal readonly struct TargetSearchData
        {
            public readonly TargetSearchPosition TargetSearchPosition;
            public readonly TargetSearchDistance TargetSearchDistance;

            public TargetSearchData(TargetSearchPosition targetSearchPosition, TargetSearchDistance targetSearchDistance)
            {
                TargetSearchPosition = targetSearchPosition;
                TargetSearchDistance = targetSearchDistance;
            }
        }
    }
}
