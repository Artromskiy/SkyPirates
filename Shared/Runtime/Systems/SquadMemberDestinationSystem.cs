using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Core.Collections;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
{
    public class SquadMemberDestinationSystem : IDeltaTickableExecutor
    {
        private Query? _unitsDescCache;
        private Query _unitsDesc => _unitsDescCache ??= _world.
            WhereAll<SquadMember, SyncId, DesiredPosition, DesiredRotation>().Alive().NotDisabled();
        private readonly OrderedQuery _orderedUnitsDesc;

        private Query? _squadsDescCache;
        private Query _squadsDesc => _squadsDescCache ??= _world.
            WhereAll<Squad, SyncId, Position, Rotation, SquadMemberCount>().Alive().NotDisabled();

        private readonly World _world;

        private readonly Lookup<SquadData> _dataPerSquad = new();
        private Lookup<SquadData> _dataState;
        private readonly IOperation<Lookup<SquadData>> _collectSquads;
        private SquadMemberOrderState _orderState;
        private readonly IOperation<SquadMemberOrderState> _applyOrder;

        public SquadMemberDestinationSystem(World world, PackedCirclesConfig circlesConfig)
        {
            _world = world;
            _dataState = _dataPerSquad;
            var squadsDesc = _squadsDesc;
            _collectSquads = world.ForEach<Lookup<SquadData>, SyncId, Position, Rotation, SquadMemberCount>(in squadsDesc, ref _dataState,
                static (ref Lookup<SquadData> data, in SyncId syncId, in Position position, in Rotation rotation, in SquadMemberCount memberCount) =>
                    data[syncId.Value] = new(position, rotation, memberCount));

            var units = _unitsDesc;
            var squadIdComparer = default(SquadIdComparer);
            var syncIdComparer = default(SyncIdComparer);
            _orderedUnitsDesc = units
                .OrderBy(ref squadIdComparer)
                .ThenBy(ref syncIdComparer);
            _orderState = new SquadMemberOrderState { DataPerSquad = _dataPerSquad, CirclesConfig = circlesConfig };
            _applyOrder = _orderedUnitsDesc.ForEach(ref _orderState,
                static (ref SquadMemberOrderState state, in SquadMember member,
                    ref DesiredPosition desiredPosition, ref DesiredRotation desiredRotation) =>
                {
                    if (!state.HasCurrentSquad || state.CurrentSquadId != member.SquadId.Value)
                    {
                        state.CurrentSquadId = member.SquadId.Value;
                        state.CurrentSquadOrder = 0;
                        state.HasCurrentSquad = true;
                    }

                    var squad = state.DataPerSquad[member.SquadId.Value];
                    var circles = state.CirclesConfig[squad.MemberCount];
                    var local = circles.Points[state.CurrentSquadOrder++];
                    desiredPosition.Value = squad.Position + local.x_y;
                    desiredRotation.Value = squad.Rotation;
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _dataPerSquad.Clear();
            _collectSquads.Invoke(ref _dataState);

            _orderState.CurrentSquadId = default;
            _orderState.CurrentSquadOrder = default;
            _orderState.HasCurrentSquad = false;
            _applyOrder.Invoke(ref _orderState);
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
        public Lookup<SquadMemberDestinationSystem.SquadData> DataPerSquad;
        public PackedCirclesConfig CirclesConfig;
        public int CurrentSquadId;
        public int CurrentSquadOrder;
        public bool HasCurrentSquad;
    }
}
