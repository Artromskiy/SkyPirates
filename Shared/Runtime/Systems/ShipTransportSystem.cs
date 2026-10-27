using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Core;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.IServices.TickableExecutors;
using SlyPirates.Shared.Ids;
using System.Collections.Generic;

namespace SlyPirates.Shared.Systems
{
    public sealed class ShipTransportSystem : IDeltaTickableExecutor
    {
        private static readonly fix BoardingDuration = (fix)0.5;
        private static readonly fix TransitionCooldownDuration = (fix)2;

        private readonly World _world;
        private readonly PackedCirclesConfig _circlesConfig;
        private readonly HashSet<ComponentId> _passengerComponents;
        private readonly ComponentId _goodsDropComponentId;
        private Entity _mapEntity;
        private readonly List<Transition> _transitions = new();
        private readonly List<Entity> _members = new();
        private readonly IResultOperation<Entity> _findMap;
        private (World World, EntityIdMap EntityIdMap, HexMap Map, List<Transition> Transitions, fix DeltaTime) _tickState;
        private readonly IOperation<(World World, EntityIdMap EntityIdMap, HexMap Map, List<Transition> Transitions, fix DeltaTime)> _processShips;
        private (SyncId SquadId, List<Entity> Members) _collectState;
        private readonly IOperation<(SyncId SquadId, List<Entity> Members)> _collectMembersOperation;
        private readonly IOperation<(SyncId SquadId, List<Entity> Members)> _collectPassengersOperation;

        private Query? _shipsCache;
        private Query Ships => _shipsCache ??= _world
            .WhereAll<Ship, SyncId, Position, Direction, Fixation, MaxSpeed>()
            .WhereNone<PendingDestroy>()
            .Alive();

        private Query? _membersCache;
        private Query Members => _membersCache ??= _world
            .WhereAll<SquadMember>()
            .WhereNone<PendingDestroy>()
            .Alive();

        private Query? _passengersCache;
        private Query Passengers => _passengersCache ??= _world
            .WhereAll<OnShip, SyncId>()
            .Alive();

        public ShipTransportSystem(World world, PackedCirclesConfig circlesConfig)
        {
            _world = world;
            _circlesConfig = circlesConfig;
            _passengerComponents = new HashSet<ComponentId>(world.GetGroup(Group.History).ToArray());
            _passengerComponents.UnionWith(world.GetGroup(Group.Temporary).ToArray());
            _passengerComponents.Remove(world.Components.GetPrimary<Alive>());
            _passengerComponents.Remove(world.Components.GetPrimary<SyncId>());
            _goodsDropComponentId = world.Components.GetPrimary<GoodsDrop>();

            Query mapQuery = world.WhereAll<HexMap>();
            _findMap = mapQuery.First();

            _tickState = (world, world.GetEntityIdMap(), default, _transitions, fix.Zero);
            Query ships = Ships;
            _processShips = world.ForEachEntity<
                (World World, EntityIdMap EntityIdMap, HexMap Map, List<Transition> Transitions, fix DeltaTime),
                Ship, SyncId, Position>(in ships, ref _tickState,
                static (ref (World World, EntityIdMap EntityIdMap, HexMap Map, List<Transition> Transitions, fix DeltaTime) state,
                    EntityRef shipRef, ref Ship ship, in SyncId shipId, in Position shipPosition) =>
                {
                    bool wasCoolingDown = ship.TransitionCooldownSeconds > fix.Zero;
                    fix boardingDeltaTime = state.DeltaTime;
                    if (wasCoolingDown)
                    {
                        boardingDeltaTime = state.DeltaTime > ship.TransitionCooldownSeconds
                            ? state.DeltaTime - ship.TransitionCooldownSeconds
                            : fix.Zero;
                        ship.TransitionCooldownSeconds = state.DeltaTime >= ship.TransitionCooldownSeconds
                            ? fix.Zero
                            : ship.TransitionCooldownSeconds - state.DeltaTime;
                    }

                    Entity squad = state.EntityIdMap.GetEntity(ship.SquadId);
                    if (!state.World.Has<Alive>(squad) || state.World.Has<PendingDestroy>(squad))
                    {
                        return;
                    }

                    bool hasOnShip = state.World.Has<OnShip>(squad);
                    if (hasOnShip && state.World.Get<OnShip>(squad).ShipId.Value != shipId.Value)
                    {
                        ship.BoardingElapsedSeconds = fix.Zero;
                        return;
                    }
                    bool isBoarded = hasOnShip;

                    if (isBoarded)
                    {
                        Position landing = default;
                        bool insideDisembarkRange = state.Map.Data != null
                            && TryFindLanding(state.Map, shipPosition.Value, ship.DisembarkDistance, out landing);
                        ship.BoardingElapsedSeconds = fix.Zero;
                        if (insideDisembarkRange && !ship.WasInsideDisembarkRange && !wasCoolingDown)
                        {
                            state.Transitions.Add(new Transition((Entity)shipRef, squad, ship.SquadId, shipId,
                                landing, TransitionKind.Disembark));
                            ship.TransitionCooldownSeconds = TransitionCooldownDuration;
                            ship.WasInsideDisembarkRange = true;
                            return;
                        }

                        ship.WasInsideDisembarkRange = insideDisembarkRange;
                        return;
                    }

                    if (fix2.SqrDistance(shipPosition.Value.xz, state.World.Get<Position>(squad).Value.xz)
                        > ship.BoardingRadius * ship.BoardingRadius)
                    {
                        ship.BoardingElapsedSeconds = fix.Zero;
                        return;
                    }

                    ship.BoardingElapsedSeconds += boardingDeltaTime;
                    if (ship.BoardingElapsedSeconds < BoardingDuration)
                    {
                        return;
                    }

                    ship.WasInsideDisembarkRange = state.Map.Data != null
                                                   && TryFindLanding(state.Map, shipPosition.Value, ship.DisembarkDistance, out _);
                    ship.BoardingElapsedSeconds = fix.Zero;
                    ship.TransitionCooldownSeconds = TransitionCooldownDuration;
                    state.Transitions.Add(new Transition((Entity)shipRef, squad, ship.SquadId, shipId,
                        default, TransitionKind.Board));
                });

            _collectState = (default, _members);
            Query members = Members;
            _collectMembersOperation = world.ForEachEntity<(SyncId SquadId, List<Entity> Members), SquadMember>(in members, ref _collectState,
                static (ref (SyncId SquadId, List<Entity> Members) state, EntityRef entity, in SquadMember member) =>
                {
                    if (member.SquadId.Value == state.SquadId.Value)
                    {
                        state.Members.Add(entity);
                    }
                });

            Query passengers = Passengers;
            _collectPassengersOperation = world.ForEachEntity<(SyncId SquadId, List<Entity> Members), OnShip, SyncId>(in passengers, ref _collectState,
                static (ref (SyncId SquadId, List<Entity> Members) state, EntityRef entity,
                    in OnShip onShip, in SyncId syncId) =>
                {
                    if (onShip.SquadId.Value == state.SquadId.Value && syncId.Value != state.SquadId.Value)
                    {
                        state.Members.Add(entity);
                    }
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _transitions.Clear();

            if (!_mapEntity.IsValid)
            {
                _mapEntity = _findMap.Invoke();
            }

            HexMap map = _mapEntity.IsValid ? _world.Get<HexMap>(_mapEntity) : default;
            EntityIdMap entityIdMap = _world.GetEntityIdMap();

            _tickState.EntityIdMap = entityIdMap;
            _tickState.Map = map;
            _tickState.DeltaTime = deltaTime;
            _processShips.Invoke(ref _tickState);

            for (int i = 0; i < _transitions.Count; i++)
            {
                Transition transition = _transitions[i];
                if (transition.Kind == TransitionKind.Board)
                {
                    Board(transition);
                }
                else
                {
                    Disembark(transition);
                }
            }
        }

        private void Board(in Transition transition)
        {
            CollectEntities(transition.SquadId, _collectMembersOperation);
            BoardEntity(transition.Squad, transition.SquadId, transition.ShipId, true);

            for (int i = 0; i < _members.Count; i++)
            {
                BoardEntity(_members[i], transition.SquadId, transition.ShipId, false);
            }
        }

        private void Disembark(in Transition transition)
        {
            CollectEntities(transition.SquadId, _collectPassengersOperation);
            _members.Sort((left, right) =>
                _world.Get<SyncId>(left).Value.CompareTo(_world.Get<SyncId>(right).Value));

            OnShip squadState = _world.Get<OnShip>(transition.Squad);
            _world.AddOrSet(transition.Squad, squadState.Components);
            _world.Remove<OnShip>(transition.Squad);
            _world.GetRef<Position>(transition.Squad) = transition.Landing;
            _world.GetRef<Direction>(transition.Squad).Value = fix2.zero;
            _world.GetRef<Fixation>(transition.Squad).Value = false;
            Rotation squadRotation = _world.Get<Rotation>(transition.Squad);

            if (_members.Count != 0)
            {
                PackedCirclesData formation = _circlesConfig[_members.Count];
                for (int i = 0; i < _members.Count; i++)
                {
                    Entity member = _members[i];
                    OnShip memberState = _world.Get<OnShip>(member);
                    _world.AddOrSet(member, memberState.Components);
                    _world.Remove<OnShip>(member);

                    fix3 position = transition.Landing.Value + formation.Points[i].x_y;
                    _world.GetRef<Position>(member) = new Position { Value = position };
                    _world.GetRef<DesiredPosition>(member) = new DesiredPosition { Value = position };
                    _world.GetRef<DesiredRotation>(member) = new DesiredRotation { Value = squadRotation.Value };
                    if (_world.Has<Direction>(member))
                    {
                        _world.GetRef<Direction>(member).Value = fix2.zero;
                    }
                }
            }

            _world.GetRef<Direction>(transition.Ship).Value = fix2.zero;
            _world.GetRef<Fixation>(transition.Ship).Value = false;
            ref Ship ship = ref _world.GetRef<Ship>(transition.Ship);
            ship.BoardingElapsedSeconds = fix.Zero;
            ship.TransitionCooldownSeconds = TransitionCooldownDuration;
        }

        private void CollectEntities(SyncId squadId, IOperation<(SyncId SquadId, List<Entity> Members)> operation)
        {
            _members.Clear();
            _collectState.SquadId = squadId;
            operation.Invoke(ref _collectState);
        }

        private void BoardEntity(Entity entity, SyncId squadId, SyncId shipId, bool isSquad)
        {
            var components = new EntityData();
            components.AddOrSet(_world, entity);
            for (int i = components.Count - 1; i >= 0; i--)
            {
                ComponentId id = components.ComponentIds[i];
                if (!_passengerComponents.Contains(id) || (isSquad && id == _goodsDropComponentId))
                {
                    components.Remove(id);
                }
            }

            _world.Remove(entity, components.ComponentIds);

            _world.Add(entity, new OnShip
            {
                ShipId = shipId,
                SquadId = squadId,
                Components = components,
            });
        }

        private static bool TryFindLanding(HexMap map, fix3 shipPosition, fix distance, out Position landing)
        {
            fix maxDistanceSquared = distance * distance;
            fix nearestDistanceSquared = maxDistanceSquared;
            int3 nearestCoordinate = default;
            bool found = false;
            landing = default;

            int2 center = Hex.WorldToAxial(shipPosition.xz);
            int range = (int)Maths.Ceil(distance / Hex.InnerRadius) + 1;
            for (int q = -range; q <= range; q++)
            {
                for (int r = -range; r <= range; r++)
                {
                    int2 column = center + new int2(q, r);
                    if (!map.SurfaceElevations.TryGetValue(column, out int elevation)
                        || elevation > 2)
                    {
                        continue;
                    }

                    int3 coordinate = new(column.x, elevation, column.y);

                    fix3 tilePosition = Hex.AxialToWorld(coordinate);
                    fix distanceSquared = fix2.SqrDistance(shipPosition.xz, tilePosition.xz);
                    if (distanceSquared > maxDistanceSquared)
                    {
                        continue;
                    }

                    if (found && (distanceSquared > nearestDistanceSquared
                                  || (distanceSquared == nearestDistanceSquared && Compare(coordinate, nearestCoordinate) >= 0)))
                    {
                        continue;
                    }

                    found = true;
                    nearestDistanceSquared = distanceSquared;
                    nearestCoordinate = coordinate;
                    landing = new Position { Value = tilePosition };
                }
            }

            return found;
        }

        private static int Compare(int3 left, int3 right)
        {
            int x = left.x.CompareTo(right.x);
            if (x != 0)
            {
                return x;
            }

            int y = left.y.CompareTo(right.y);
            return y != 0 ? y : left.z.CompareTo(right.z);
        }

        private enum TransitionKind : byte { Board, Disembark }

        private readonly struct Transition
        {
            public readonly Entity Ship;
            public readonly Entity Squad;
            public readonly SyncId SquadId;
            public readonly SyncId ShipId;
            public readonly Position Landing;
            public readonly TransitionKind Kind;

            public Transition(Entity ship, Entity squad, SyncId squadId, SyncId shipId, Position landing,
                TransitionKind kind)
            {
                Ship = ship;
                Squad = squad;
                SquadId = squadId;
                ShipId = shipId;
                Landing = landing;
                Kind = kind;
            }
        }
    }
}
