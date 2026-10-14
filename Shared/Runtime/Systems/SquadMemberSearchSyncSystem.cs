using Delta;
using Delta.ECS;
using SkyPirates.Shared.Ecs;
using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Core.Collections;
using SkyPirates.Shared.Components.Config;
using SkyPirates.Shared.Components.Framed;
using SkyPirates.Shared.IServices.TickableExecutors;

namespace SkyPirates.Shared.Systems
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

        public SquadMemberSearchSyncSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            _searchDataPerSquad.Clear();

            var searchData = _searchDataPerSquad;
            var squadsDesc = _squadsDesc;
            _world.ForEach<Lookup<TargetSearchData>, SyncId, TargetSearchPosition, TargetSearchDistance, Fixation>(in squadsDesc, ref searchData,
                static (ref Lookup<TargetSearchData> dataPerSquad, in SyncId syncId, in TargetSearchPosition searchPosition, in TargetSearchDistance searchDistance, in Fixation fixation) =>
                {
                    TargetSearchDistance distance = fixation ? fix.Zero : searchDistance;
                    dataPerSquad[syncId.Value] = new(searchPosition, distance);
                }).Invoke(ref searchData);

            var unitsDesc = _unitsDesc;
            _world.ForEach<Lookup<TargetSearchData>, SquadMember, TargetSearchPosition, TargetSearchDistance>(in unitsDesc, ref searchData,
                static (ref Lookup<TargetSearchData> dataPerSquad, in SquadMember squadMember, ref TargetSearchPosition searchPosition, ref TargetSearchDistance searchDistance) =>
                {
                    dataPerSquad.TryGetValue(squadMember.SquadId, out var data);
                    searchPosition = data.TargetSearchPosition;
                    searchDistance = data.TargetSearchDistance;
                }).Invoke(ref searchData);
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
