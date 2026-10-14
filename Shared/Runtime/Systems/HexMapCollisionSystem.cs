using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Core;
using DVG.SkyPirates.Shared.Core.Physics;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DVG.SkyPirates.Shared.Systems
{
    public sealed class HexMapCollisionSystem : ITransientDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Position, CachePosition, Radius, Collide>().Alive().NotDisabled();

        public static event Action<Segment[], fix2, fix2, fix>? OnFailedToSolve;
        private readonly ThreadLocal<List<Segment>> _segmentsCache = new(() => new List<Segment>());
        private readonly World _world;

        public HexMapCollisionSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            var hexMapEntity = _world.WhereAll<HexMap>()
                .First()
                .Invoke();
            if (!hexMapEntity.IsValid)
            {
                return;
            }

            var hexMap = _world.Get<HexMap>(hexMapEntity);
            if (hexMap.Data == null)
            {
                return;
            }

            (HexMap HexMap, ThreadLocal<List<Segment>> SegmentsCache) state = (hexMap, _segmentsCache);
            var desc = _desc;
            _world.ForEach<(HexMap HexMap, ThreadLocal<List<Segment>> SegmentsCache), Position, CachePosition, Radius>(in desc, ref state,
                static (ref (HexMap HexMap, ThreadLocal<List<Segment>> SegmentsCache) state, ref Position position, ref CachePosition cachePosition, in Radius radius) =>
                {
                    var segments = state.SegmentsCache.Value;
                    segments.Clear();
                    FindSegments(state.HexMap, segments, cachePosition);
                    Solvers.Segments = segments.ToArray();
                    fix2 solvedPos = fix2.zero;
                    bool failed = false;
                    try
                    {
                        solvedPos = Solvers.CircleSlide(cachePosition.Value.xz, position.Value.xz - cachePosition.Value.xz, radius);
                    }
                    catch { failed = true; }
                    position.Value.xz = solvedPos;
                    failed |= !Walkable(state.HexMap, Hex.WorldToAxial(position.Value));
                    if (failed)
                    {
                        position.Value = cachePosition;
                        OnFailedToSolve?.Invoke(Solvers.Segments.ToArray(), cachePosition.Value.xz, position.Value.xz, radius);
                    }
                    //Delta.Diagnostics.Trace.Assert(!failed, "Failed to solve collision");
                }).Invoke(ref state);
        }

        private static void FindSegments(HexMap hexMap, List<Segment> segments, fix3 from)
        {
            var axialFrom = Hex.WorldToAxial(from);

            foreach (var item in Hex.AxialNear)
            {
                var offsetted = item.x_y + axialFrom;
                if (Walkable(hexMap, offsetted))
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

        private static bool Walkable(HexMap hexMap, int3 axial)
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
