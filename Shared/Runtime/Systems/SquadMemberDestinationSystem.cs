using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Components;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Core.Collections;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;

namespace DVG.SkyPirates.Shared.Systems
{
    public class SquadMemberDestinationSystem : ITransientDeltaTickableExecutor
    {
        private Query? _unitsDescCache;
        private Query _unitsDesc => _unitsDescCache ??= _world.
            WhereAll<SquadMember, SyncId, DesiredPosition, DesiredRotation>().Alive().NotDisabled();
        private readonly OrderedQuery _orderedUnitsDesc;

        private Query? _squadsDescCache;
        private Query _squadsDesc => _squadsDescCache ??= _world.
            WhereAll<Squad, SyncId, Position, Rotation, SquadMemberCount>().Alive().NotDisabled();

        private readonly World _world;
        private readonly PackedCirclesConfig _circlesConfig;

        private readonly Lookup<int> _orderPerUnit = new();
        private readonly Lookup<SquadData> _dataPerSquad = new();

        public SquadMemberDestinationSystem(World world, PackedCirclesConfig circlesConfig)
        {
            _world = world;
            _circlesConfig = circlesConfig;
            var units = _unitsDesc;
            var squadIdComparer = default(SquadIdComparer);
            var syncIdComparer = default(SyncIdComparer);
            _orderedUnitsDesc = units
                .OrderBy(ref squadIdComparer)
                .ThenBy(ref syncIdComparer);
        }

        public void Tick(int tick, fix deltaTime)
        {
            _orderPerUnit.Clear();
            _dataPerSquad.Clear();

            var dataPerSquad = _dataPerSquad;
            var squadsDesc = _squadsDesc;
            _world.ForEach<Lookup<SquadData>, SyncId, Position, Rotation, SquadMemberCount>(in squadsDesc, ref dataPerSquad,
                static (ref Lookup<SquadData> data, in SyncId syncId, in Position position, in Rotation rotation, in SquadMemberCount memberCount) =>
                    data[syncId.Value] = new(position, rotation, memberCount)).Invoke(ref dataPerSquad);

            var orderState = new SquadMemberOrderState { OrderPerUnit = _orderPerUnit };
            _orderedUnitsDesc.ForEach(ref orderState,
                static (ref SquadMemberOrderState state, in SquadMember member, in SyncId syncId) =>
                {
                    if (!state.HasCurrentSquad || state.CurrentSquadId != member.SquadId.Value)
                    {
                        state.CurrentSquadId = member.SquadId.Value;
                        state.CurrentSquadOrder = 0;
                        state.HasCurrentSquad = true;
                    }

                    state.OrderPerUnit[syncId.Value] = state.CurrentSquadOrder++;
                }).Invoke(ref orderState);

            (Lookup<int> OrderPerUnit, Lookup<SquadData> DataPerSquad, PackedCirclesConfig CirclesConfig) applyState = (_orderPerUnit, _dataPerSquad, _circlesConfig);
            var unitsDesc = _unitsDesc;
            _world.ForEach<(Lookup<int> OrderPerUnit, Lookup<SquadData> DataPerSquad, PackedCirclesConfig CirclesConfig), SyncId, SquadMember, DesiredPosition, DesiredRotation>(in unitsDesc, ref applyState,
                static (ref (Lookup<int> OrderPerUnit, Lookup<SquadData> DataPerSquad, PackedCirclesConfig CirclesConfig) state, in SyncId syncId, in SquadMember member, ref DesiredPosition desiredPosition, ref DesiredRotation desiredRotation) =>
                {
                    var squad = state.DataPerSquad[member.SquadId.Value];
                    var circles = state.CirclesConfig[squad.MemberCount];
                    int order = state.OrderPerUnit[syncId.Value];
                    var local = circles.Points[order];
                    desiredPosition.Value = squad.Position + local.x_y;
                    desiredRotation.Value = squad.Rotation;
                }).Invoke(ref applyState);
        }

        internal readonly struct SquadData
        {
            public readonly Position Position;
            public readonly Rotation Rotation;
            public readonly SquadMemberCount MemberCount;

            public SquadData(Position position, Rotation rotation, SquadMemberCount memberCount)
            {
                Position = position;
                Rotation = rotation;
                MemberCount = memberCount;
            }
        }

    }

    internal struct SquadMemberOrderState
    {
        public Lookup<int> OrderPerUnit;
        public int CurrentSquadId;
        public int CurrentSquadOrder;
        public bool HasCurrentSquad;
    }
}
