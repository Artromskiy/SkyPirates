using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Core;
using SlyPirates.Shared.Core.Physics;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SlyPirates.Shared.Systems
{
    public sealed class HexMapCollisionSystem : IDeltaTickableExecutor
    {
        private const int MaxClimbElevation = 2;

        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Position, CachePosition, Radius, Collide>().Alive().NotDisabled();

        public static event Action<Segment[], fix2, fix2, fix>? OnFailedToSolve;
        private readonly ThreadLocal<SegmentBuffer> _segmentsCache = new(() => new SegmentBuffer());
        private readonly World _world;
        private readonly IResultOperation<Entity> _findMap;
        private (HexMap HexMap, ThreadLocal<SegmentBuffer> SegmentsCache) _state;
        private readonly IOperation<(HexMap HexMap, ThreadLocal<SegmentBuffer> SegmentsCache)> _solveMovement;

        public HexMapCollisionSystem(World world)
        {
            _world = world;
            Query mapQuery = world.WhereAll<HexMap>();
            _findMap = mapQuery.First();

            _state = (default, _segmentsCache);
            var desc = _desc;
            _solveMovement = world.ForEach<(HexMap HexMap, ThreadLocal<SegmentBuffer> SegmentsCache), Position, CachePosition, Radius>(in desc, ref _state,
                static (ref (HexMap HexMap, ThreadLocal<SegmentBuffer> SegmentsCache) state, ref Position position, in CachePosition cachePosition, in Radius radius) =>
                {
                    int3 source = Hex.WorldToAxial(cachePosition.Value);
                    int sourceElevation = source.y;

                    SegmentBuffer buffer = state.SegmentsCache.Value;
                    var segments = buffer.Segments;
                    segments.Clear();
                    FindSegments(state.HexMap, segments, source.xz, sourceElevation);
                    if (buffer.Array.Length < segments.Count)
                    {
                        Array.Resize(ref buffer.Array, Math.Max(segments.Count, buffer.Array.Length * 2));
                    }
                    segments.CopyTo(buffer.Array);
                    Solvers.Segments = new Memory<Segment>(buffer.Array, 0, segments.Count);

                    fix2 solvedPos = fix2.zero;
                    bool failed = false;
                    try
                    {
                        solvedPos = Solvers.CircleSlide(cachePosition.Value.xz, position.Value.xz - cachePosition.Value.xz, radius);
                    }
                    catch { failed = true; }
                    position.Value.xz = solvedPos;
                    if (!failed
                        && TryGetSupportElevation(state.HexMap, solvedPos, radius, sourceElevation, out int destinationElevation))
                    {
                        position.Value.y = Hex.AxialToWorldY(destinationElevation);
                    }
                    else
                    {
                        failed = true;
                    }

                    if (failed)
                    {
                        position.Value = cachePosition;
                        OnFailedToSolve?.Invoke(Solvers.Segments.ToArray(), cachePosition.Value.xz, position.Value.xz, radius);
                    }
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            var hexMapEntity = _findMap.Invoke();
            if (!hexMapEntity.IsValid)
            {
                return;
            }

            var hexMap = _world.Get<HexMap>(hexMapEntity);
            if (hexMap.Data == null)
            {
                return;
            }

            _state.HexMap = hexMap;
            _solveMovement.Invoke(ref _state);
        }

        private static void FindSegments(HexMap hexMap, List<Segment> segments, int2 from, int sourceElevation)
        {
            foreach (var item in Hex.AxialNear)
            {
                int2 neighbor = item + from;
                if (TryGetSurfaceElevation(hexMap, neighbor, out int destinationElevation)
                    && destinationElevation - sourceElevation <= MaxClimbElevation)
                {
                    continue;
                }

                var worldFloor = Hex.AxialToWorld(neighbor);
                for (int i = 0; i < Hex.Points.Length; i++)
                {
                    var s = worldFloor + Hex.Points[i];
                    var e = worldFloor + Hex.Points[(i + 1) % Hex.Points.Length];
                    segments.Add(new(s, e));
                }
            }
        }

        private sealed class SegmentBuffer
        {
            public readonly List<Segment> Segments = new();
            public Segment[] Array = System.Array.Empty<Segment>();
        }

        private static bool TryGetSupportElevation(HexMap map, fix2 position, fix radius, int sourceElevation, out int elevation)
        {
            int2 column = Hex.WorldToAxial(position);
            int maxElevation = sourceElevation + MaxClimbElevation;
            fix sqrRadius = radius * radius;
            bool found = TryGetSurfaceElevation(map, column, out elevation) && elevation <= maxElevation;
            if (!found)
            {
                elevation = int.MinValue;
            }

            foreach (int2 offset in Hex.AxialNear)
            {
                int2 neighbor = column + offset;
                if (!TryGetSurfaceElevation(map, neighbor, out int height) || height <= elevation || height > maxElevation)
                {
                    continue;
                }

                // Fold the hex into its first quadrant: only the top edge and one diagonal remain.
                fix2 local = position - Hex.AxialToWorld(neighbor);
                local = new fix2(Maths.Abs(local.x), Maths.Abs(local.y));
                fix2 closest = local.x <= Hex.OuterRadius / 2
                    ? new fix2(local.x, Hex.InnerRadius)
                    : Spatial.Projection(local, Hex.Points[0], Hex.Points[1]);
                if (fix2.SqrDistance(local, closest) <= sqrRadius)
                {
                    elevation = height;
                    found = true;
                }
            }

            return found;
        }

        internal static bool IsWalkable(HexMap hexMap, int3 axial)
        {
            return TryGetSurfaceElevation(hexMap, axial.xz, out int elevation)
                && Math.Abs((long)axial.y - elevation) <= 1;
        }

        private static bool TryGetSurfaceElevation(HexMap hexMap, int2 column, out int elevation)
        {
            if (hexMap.SurfaceElevations != null)
            {
                return hexMap.SurfaceElevations.TryGetValue(column, out elevation);
            }

            // Keep direct callers working with maps created without HexMapUpdateSystem.
            if (hexMap.Data != null)
            {
                bool found = false;
                elevation = int.MinValue;
                foreach (int3 coordinate in hexMap.Data.Keys)
                {
                    if (coordinate.x == column.x && coordinate.z == column.y && coordinate.y > elevation)
                    {
                        elevation = coordinate.y;
                        found = true;
                    }
                }

                if (found)
                {
                    return true;
                }
            }

            elevation = default;
            return false;
        }
    }
}
