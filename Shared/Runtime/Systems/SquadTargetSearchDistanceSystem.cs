using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace SlyPirates.Shared.Systems
{
    public class SquadTargetSearchDistanceSystem : IDeltaTickableExecutor
    {
        private static readonly fix _baseRange = 1;

        private Query? _squadsMembersCache;
        private Query _squadsMembers => _squadsMembersCache ??= _world.
            WhereAll<SquadMember, ImpactDistance>().Alive().NotDisabled();

        private Query? _squadsDescCache;
        private Query _squadsDesc => _squadsDescCache ??= _world.
            WhereAll<Squad, SyncId, SquadMemberCount, TargetSearchDistance>().Alive().NotDisabled();

        private readonly Dictionary<int, fix> _maxImpactDistancePerSquad = new();
        private readonly World _world;
        private Dictionary<int, fix> _distanceState;
        private readonly IOperation<Dictionary<int, fix>> _collectImpactDistances;
        private (PackedCirclesConfig Config, Dictionary<int, fix> MaxImpactDistancePerSquad) _applyState;
        private readonly IOperation<(PackedCirclesConfig Config, Dictionary<int, fix> MaxImpactDistancePerSquad)> _applySearchDistances;


        public SquadTargetSearchDistanceSystem(PackedCirclesConfig circlesConfig, World world)
        {
            _world = world;
            _distanceState = _maxImpactDistancePerSquad;
            var members = _squadsMembers;
            _collectImpactDistances = world.ForEach<Dictionary<int, fix>, SquadMember, ImpactDistance>(in members, ref _distanceState,
                static (ref Dictionary<int, fix> distances, in SquadMember member, in ImpactDistance impactDistance) =>
                {
                    var currentImpactDistance = distances.GetValueOrDefault(member.SquadId.Value);
                    distances[member.SquadId.Value] = Maths.Max(currentImpactDistance, impactDistance);
                });

            _applyState = (circlesConfig, _maxImpactDistancePerSquad);
            var squads = _squadsDesc;
            _applySearchDistances = world.ForEach<(PackedCirclesConfig Config, Dictionary<int, fix> MaxImpactDistancePerSquad), SyncId, SquadMemberCount, TargetSearchDistance>(in squads, ref _applyState,
                static (ref (PackedCirclesConfig Config, Dictionary<int, fix> MaxImpactDistancePerSquad) state, in SyncId syncId, in SquadMemberCount memberCount, ref TargetSearchDistance searchDistance) =>
                {
                    fix squadRadius = memberCount == 0 ? 0 : state.Config[memberCount].Radius;
                    var maxImpactDistance = state.MaxImpactDistancePerSquad.GetValueOrDefault(syncId);
                    searchDistance = memberCount == 0
                        ? fix.Zero
                        : maxImpactDistance + _baseRange + squadRadius;
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _maxImpactDistancePerSquad.Clear();
            _collectImpactDistances.Invoke(ref _distanceState);
            _applySearchDistances.Invoke(ref _applyState);
        }
    }
}
