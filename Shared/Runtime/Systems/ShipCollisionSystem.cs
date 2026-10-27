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

namespace SlyPirates.Shared.Systems
{
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
                    ReadOnlySpan<int2> neighbors = Hex.AxialNear;
                    int2[] offsets = state.GetSearchOffsets(range);
                    for (int i = 0; i < offsets.Length; i++)
                    {
                        int2 column = center + offsets[i];
                        if (!state.Map.SurfaceElevations.ContainsKey(column))
                        {
                            continue;
                        }

                        AddBoundarySegments(state.Map.SurfaceElevations, column, neighbors, state.Segments);
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

        private static void AddBoundarySegments(Dictionary<int2, int> surfaceElevations, int2 column,
            ReadOnlySpan<int2> neighbors, List<Segment> segments)
        {
            fix2 tilePosition = Hex.AxialToWorld(column);
            ReadOnlySpan<fix2> points = Hex.Points;
            for (int edge = 0; edge < points.Length; edge++)
            {
                int next = (edge + 1) % points.Length;
                if (surfaceElevations.ContainsKey(column + neighbors[next]))
                {
                    continue;
                }

                segments.Add(new Segment(tilePosition + points[edge], tilePosition + points[next]));
            }
        }

        private sealed class CollisionState
        {
            private readonly Dictionary<int, int2[]> _searchOffsetsByRange = new();

            public HexMap Map;
            public readonly List<Segment> Segments = new();
            public Segment[] Buffer = Array.Empty<Segment>();

            public int2[] GetSearchOffsets(int range)
            {
                if (_searchOffsetsByRange.TryGetValue(range, out int2[] offsets))
                {
                    return offsets;
                }

                var result = new List<int2>();
                for (int q = -range; q <= range; q++)
                {
                    int minR = q < 0 ? -range - q : -range;
                    int maxR = q > 0 ? range - q : range;
                    for (int r = minR; r <= maxR; r++)
                    {
                        result.Add(new int2(q, r));
                    }
                }

                offsets = result.ToArray();
                _searchOffsetsByRange.Add(range, offsets);
                return offsets;
            }
        }
    }
}
