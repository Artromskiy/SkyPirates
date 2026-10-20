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
                    SegmentBuffer buffer = state.SegmentsCache.Value;
                    var segments = buffer.Segments;
                    segments.Clear();
                    FindSegments(state.HexMap, segments, cachePosition);
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
                    failed |= !IsWalkable(state.HexMap, Hex.WorldToAxial(position.Value));
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

        private static void FindSegments(HexMap hexMap, List<Segment> segments, fix3 from)
        {
            var axialFrom = Hex.WorldToAxial(from);

            foreach (var item in Hex.AxialNear)
            {
                var offsetted = item.x_y + axialFrom;
                if (IsWalkable(hexMap, offsetted))
                {
                    continue;
                }

                var worldFloor = Hex.AxialToWorld(offsetted.xz);
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

        internal static bool IsWalkable(HexMap hexMap, int3 axial)
        {
            bool zero = hexMap.Data.ContainsKey(axial);
            var up = new int3(0, 1, 0);
            bool p1 = hexMap.Data.ContainsKey(axial + up);
            bool p2 = hexMap.Data.ContainsKey(axial + up * 2);
            bool p3 = hexMap.Data.ContainsKey(axial + up * 3);
            bool m1 = hexMap.Data.ContainsKey(axial - up);
            return (zero && !p1 && !p2) || (p1 && !p2 && !p3) || (m1 && !zero && !p1);
        }
    }
}
