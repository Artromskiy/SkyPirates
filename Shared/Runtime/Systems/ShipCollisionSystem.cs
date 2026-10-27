using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Core;
using SlyPirates.Shared.Core.Physics;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections.Generic;
using Delta.Diagnostics;

namespace SlyPirates.Shared.Systems
{
    [AgentTip("Class is overcomplicated in terms of math especially 3-depth-loop")]
    public sealed class ShipCollisionSystem : IDeltaTickableExecutor
    {
        private readonly World _world;
        private readonly IResultOperation<Entity> _findMap;
        private CollisionState _state = new();
        private readonly IOperation<CollisionState> _collide;

        public ShipCollisionSystem(World world)
        {
            _world = world;
            _findMap = world.WhereAll<HexMap>().First();
            Query ships = world.WhereAll<Ship, Position, CachePosition, Radius>().Alive().NotDisabled();
            _collide = world.ForEach<CollisionState, Position, CachePosition, Radius>(in ships, ref _state,
                static (ref CollisionState state, ref Position position, in CachePosition previous, in Radius radius) =>
                {
                    fix2 from = previous.Value.xz;
                    fix2 to = position.Value.xz;
                    if (from == to)
                    {
                        return;
                    }

                    state.Segments.Clear();
                    int2 center = Hex.WorldToAxial(from);
                    int range = (int)Maths.Ceil((fix2.Distance(from, to) + radius.Value + Hex.OuterRadius) / Hex.InnerRadius);
                    for (int q = -range; q <= range; q++)
                    {
                        for (int r = -range; r <= range; r++)
                        {
                            int2 column = center + new int2(q, r);
                            if (!state.Map.SurfaceElevations.ContainsKey(column))
                            {
                                continue;
                            }

                            fix2 tilePosition = Hex.AxialToWorld(column);
                            for (int edge = 0; edge < Hex.Points.Length; edge++)
                            {
                                if (state.Map.SurfaceElevations.ContainsKey(column + Hex.AxialNear[(edge + 1) % 6]))
                                {
                                    continue;
                                }

                                state.Segments.Add(new Segment(tilePosition + Hex.Points[edge],
                                    tilePosition + Hex.Points[(edge + 1) % 6]));
                            }
                        }
                    }

                    if (state.Buffer.Length < state.Segments.Count)
                    {
                        Array.Resize(ref state.Buffer, Math.Max(state.Segments.Count, state.Buffer.Length * 2));
                    }
                    state.Segments.CopyTo(state.Buffer);
                    Solvers.Segments = new Memory<Segment>(state.Buffer, 0, state.Segments.Count);
                    fix2 solved = Solvers.CircleSlide(from, to - from, radius);
                    position.Value = state.Map.SurfaceElevations.ContainsKey(Hex.WorldToAxial(solved))
                        ? previous.Value : solved.x_y;
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            Entity map = _findMap.Invoke();
            if (!map.IsValid)
            {
                return;
            }

            _state.Map = _world.Get<HexMap>(map);
            _collide.Invoke(ref _state);
        }

        private sealed class CollisionState
        {
            public HexMap Map;
            public readonly List<Segment> Segments = new();
            public Segment[] Buffer = Array.Empty<Segment>();
        }
    }
}
