using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Core;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IServices.TickableExecutors;
using SlyPirates.Shared.Ids;
using System.Collections.Generic;
using SlyPirates.Shared.Ecs;

namespace SlyPirates.Shared.Systems
{
    public sealed class ShipTransportSystem : IDeltaTickableExecutor
    {
        private const int BoardingTicks = Constants.TicksPerSecond / 2;
        private const int TransitionCooldownTicks = Constants.TicksPerSecond * 2;

        private readonly World _world;
        private readonly PackedCirclesConfig _circlesConfig;
        private readonly ComponentId[] _retainedComponents;
        private Entity _mapEntity;
        private readonly List<Transition> _transitions = new();
        private readonly List<Entity> _members = new();

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
            _retainedComponents = new[]
            {
                world.Components.GetPrimary<Alive>(),
                world.Components.GetPrimary<SyncId>(),
            };
        }

        public void Tick(int tick, fix deltaTime)
        {
            _transitions.Clear();

            if (!_mapEntity.IsValid)
            {
                _mapEntity = _world.WhereAll<HexMap>().First().Invoke();
            }

            HexMap map = _mapEntity.IsValid ? _world.Get<HexMap>(_mapEntity) : default;
            EntityIdMap entityIdMap = _world.GetEntityIdMap();

            var state = (World: _world, EntityIdMap: entityIdMap, Map: map, Transitions: _transitions, Tick: tick);
            Query ships = Ships;
            _world.ForEachEntity<(World World, EntityIdMap EntityIdMap, HexMap Map, List<Transition> Transitions, int Tick),
                Ship, SyncId, Position>(
                in ships,
                ref state,
                static (ref (World World, EntityIdMap EntityIdMap, HexMap Map, List<Transition> Transitions, int Tick) state,
                    EntityRef shipRef, ref Ship ship, in SyncId shipId, in Position shipPosition) =>
                {
                    Entity squad = state.EntityIdMap.GetEntity(ship.SquadId);
                    if (!state.World.Has<Alive>(squad) || state.World.Has<PendingDestroy>(squad))
                    {
                        return;
                    }

                    bool hasOnShip = state.World.Has<OnShip>(squad);
                    if (hasOnShip && state.World.Get<OnShip>(squad).ShipId.Value != shipId.Value)
                    {
                        ship.BoardingTicks = 0;
                        return;
                    }
                    bool isBoarded = hasOnShip;

                    if (isBoarded)
                    {
                        Position landing = default;
                        bool insideDisembarkRange = state.Map.Data != null
                            && TryFindLanding(state.Map, shipPosition.Value, ship.DisembarkDistance, out landing);
                        ship.BoardingTicks = 0;
                        if (insideDisembarkRange && !ship.WasInsideDisembarkRange
                            && state.Tick >= ship.TransitionReadyTick)
                        {
                            state.Transitions.Add(new Transition((Entity)shipRef, squad, ship.SquadId, shipId,
                                landing, TransitionKind.Disembark));
                            ship.TransitionReadyTick = state.Tick + TransitionCooldownTicks;
                            ship.WasInsideDisembarkRange = true;
                            return;
                        }

                        ship.WasInsideDisembarkRange = insideDisembarkRange;
                        return;
                    }

                    if (state.Tick < ship.TransitionReadyTick
                        || fix2.SqrDistance(shipPosition.Value.xz, state.World.Get<Position>(squad).Value.xz)
                            > ship.BoardingRadius * ship.BoardingRadius)
                    {
                        ship.BoardingTicks = 0;
                        return;
                    }

                    if (++ship.BoardingTicks < BoardingTicks)
                    {
                        return;
                    }

                    ship.WasInsideDisembarkRange = state.Map.Data != null
                                                   && TryFindLanding(state.Map, shipPosition.Value, ship.DisembarkDistance, out _);
                    ship.BoardingTicks = 0;
                    ship.TransitionReadyTick = state.Tick + TransitionCooldownTicks;
                    state.Transitions.Add(new Transition((Entity)shipRef, squad, ship.SquadId, shipId,
                        default, TransitionKind.Board));
                }).Invoke(ref state);

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
            CollectMembers(transition.SquadId);
            BoardEntity(transition.Squad, transition.SquadId, transition.ShipId);

            for (int i = 0; i < _members.Count; i++)
            {
                BoardEntity(_members[i], transition.SquadId, transition.ShipId);
            }
        }

        private void Disembark(in Transition transition)
        {
            CollectPassengers(transition.SquadId);
            _members.Sort((left, right) =>
                _world.Get<SyncId>(left).Value.CompareTo(_world.Get<SyncId>(right).Value));

            OnShip squadState = _world.Get<OnShip>(transition.Squad);
            _world.SetEntityData(transition.Squad, squadState.Components);
            _world.Remove<OnShip>(transition.Squad);
            _world.GetRef<Position>(transition.Squad) = transition.Landing;
            _world.GetRef<Direction>(transition.Squad).Value = fix2.zero;
            _world.GetRef<Fixation>(transition.Squad).Value = false;
            _world.GetRef<DesiredPosition>(transition.Squad) = new DesiredPosition { Value = transition.Landing.Value };

            Rotation squadRotation = _world.Get<Rotation>(transition.Squad);
            _world.GetRef<DesiredRotation>(transition.Squad) = new DesiredRotation { Value = squadRotation.Value };

            if (_members.Count != 0)
            {
                PackedCirclesData formation = _circlesConfig[_members.Count];
                for (int i = 0; i < _members.Count; i++)
                {
                    Entity member = _members[i];
                    OnShip memberState = _world.Get<OnShip>(member);
                    _world.SetEntityData(member, memberState.Components);
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
            _world.GetRef<Ship>(transition.Ship).BoardingTicks = 0;
        }

        private void CollectMembers(SyncId squadId)
        {
            _members.Clear();
            (SyncId SquadId, List<Entity> Members) state = (squadId, _members);
            Query members = Members;
            _world.ForEachEntity<(SyncId SquadId, List<Entity> Members), SquadMember>(in members, ref state,
                static (ref (SyncId SquadId, List<Entity> Members) state, EntityRef entity, in SquadMember member) =>
                {
                    if (member.SquadId.Value == state.SquadId.Value)
                    {
                        state.Members.Add(entity);
                    }
                }).Invoke(ref state);
        }

        private void CollectPassengers(SyncId squadId)
        {
            _members.Clear();
            (SyncId SquadId, List<Entity> Members) state = (squadId, _members);
            Query passengers = Passengers;
            _world.ForEachEntity<(SyncId SquadId, List<Entity> Members), OnShip, SyncId>(in passengers, ref state,
                static (ref (SyncId SquadId, List<Entity> Members) state, EntityRef entity,
                    in OnShip onShip, in SyncId syncId) =>
                {
                    if (onShip.SquadId.Value == state.SquadId.Value && syncId.Value != state.SquadId.Value)
                    {
                        state.Members.Add(entity);
                    }
                }).Invoke(ref state);
        }

        private void BoardEntity(Entity entity, SyncId squadId, SyncId shipId)
        {
            ComponentDataSet components = ComponentDataSet.Capture(_world, entity, _retainedComponents);
            ComponentId[] componentIds = components.GetComponentIds(_world);
            if (componentIds.Length != 0)
            {
                _world.Remove(entity, componentIds);
            }

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

            foreach (KeyValuePair<int3, TileId> tile in map.Data)
            {
                int3 coordinate = tile.Key;
                if (!HexMapCollisionSystem.IsWalkable(map, coordinate))
                {
                    continue;
                }

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
