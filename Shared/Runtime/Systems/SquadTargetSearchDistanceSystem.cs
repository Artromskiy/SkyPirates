using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Systems
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
        private readonly PackedCirclesConfig _circlesConfig;
        private readonly World _world;


        public SquadTargetSearchDistanceSystem(PackedCirclesConfig circlesConfig, World world)
        {
            _circlesConfig = circlesConfig;
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            _maxImpactDistancePerSquad.Clear();
            var maxDistances = _maxImpactDistancePerSquad;
            var members = _squadsMembers;
            _world.ForEach<Dictionary<int, fix>, SquadMember, ImpactDistance>(in members, ref maxDistances,
                static (ref Dictionary<int, fix> distances, in SquadMember member, in ImpactDistance impactDistance) =>
                {
                    var currentImpactDistance = distances.GetValueOrDefault(member.SquadId.Value);
                    distances[member.SquadId.Value] = Maths.Max(currentImpactDistance, impactDistance);
                }).Invoke(ref maxDistances);

            (PackedCirclesConfig Config, Dictionary<int, fix> MaxImpactDistancePerSquad) applyState = (_circlesConfig, _maxImpactDistancePerSquad);
            var squads = _squadsDesc;
            _world.ForEach<(PackedCirclesConfig Config, Dictionary<int, fix> MaxImpactDistancePerSquad), SyncId, SquadMemberCount, TargetSearchDistance>(in squads, ref applyState,
                static (ref (PackedCirclesConfig Config, Dictionary<int, fix> MaxImpactDistancePerSquad) state, in SyncId syncId, in SquadMemberCount memberCount, ref TargetSearchDistance searchDistance) =>
                {
                    fix squadRadius = memberCount == 0 ? 0 : state.Config[memberCount].Radius;
                    var maxImpactDistance = state.MaxImpactDistancePerSquad.GetValueOrDefault(syncId);
                    searchDistance = memberCount == 0
                        ? fix.Zero
                        : maxImpactDistance + _baseRange + squadRadius;
                }).Invoke(ref applyState);
        }
    }
}
