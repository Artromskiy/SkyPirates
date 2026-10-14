using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.Components;
using DVG.Core.Collections;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Systems
{
    public sealed class SeparationSystem : ITransientDeltaTickableExecutor
    {
        public static int SquareSize = 4;

        private Query? _separatorDescCache;
        private Query _separatorDesc => _separatorDescCache ??= _world.
            WhereAll<SyncId, Position, Separator, Radius>().
            Alive().NotDisabled();

        private Query? _separationDescCache;
        private Query _separationDesc => _separationDescCache ??= _world.
            WhereAll<Position, Separation>().
            Alive().NotDisabled();

        private readonly Lookup2D<List<SeparatorEntry>> _partitioning = new();
        private readonly HashSet<int> _syncIdCache = new();

        private readonly World _world;

        public SeparationSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            _partitioning.Clear();

            var partitioning = _partitioning;
            var separatorDesc = _separatorDesc;
            _world.ForEach<Lookup2D<List<SeparatorEntry>>, SyncId, Position, Separator, Radius>(in separatorDesc, ref partitioning,
                static (ref Lookup2D<List<SeparatorEntry>> grid, in SyncId syncId, in Position position, in Separator separator, in Radius radius) =>
                {
                    var pos = ((fix3)position).xz;
                    var range = new fix2(radius.Value);
                    var minQ = GetQuantizedSquare(pos - range);
                    var maxQ = GetQuantizedSquare(pos + range);

                    for (int y = minQ.y; y <= maxQ.y; y++)
                    for (int x = minQ.x; x <= maxQ.x; x++)
                    {
                        if (!grid.TryGetValue(x, y, out var list))
                        {
                            grid[x, y] = list = new List<SeparatorEntry>(8);
                        }

                        list.Add(new SeparatorEntry(syncId, position, separator, radius.Value));
                    }
                }).Invoke(ref partitioning);

            (HashSet<int> Written, Lookup2D<List<SeparatorEntry>> Grid) state = (_syncIdCache, _partitioning);
            var separationDesc = _separationDesc;
            _world.ForEach<(HashSet<int> Written, Lookup2D<List<SeparatorEntry>> Grid), Position, Separation>(in separationDesc, ref state,
                static (ref (HashSet<int> Written, Lookup2D<List<SeparatorEntry>> Grid) state, ref Position position, ref Separation separation) =>
                {
                    state.Written.Clear();
                    var pos = ((fix3)position).xz;
                    var q = GetQuantizedSquare(pos);
                    fix2 totalForce = fix2.zero;
                    int forcesCount = 0;
                    if (!state.Grid.TryGetValue(q.x, q.y, out var list))
                    {
                        return;
                    }

                    int count = list.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var other = list[i];
                        if (!state.Written.Add(other.SyncId))
                        {
                            continue;
                        }

                        var dir = pos - other.Position;
                        var sqrDistance = fix2.SqrLength(dir);
                        if (sqrDistance == 0)
                        {
                            continue;
                        }

                        var maxDistance = other.Radius + other.SeparatorRadius;
                        var maxSqrDistance = maxDistance * maxDistance;
                        if (sqrDistance > maxSqrDistance)
                        {
                            continue;
                        }

                        var distance = Maths.Sqrt(sqrDistance);
                        dir /= distance;
                        var softForce = Maths.Clamp(Maths.InvLerp(maxDistance, other.Radius, distance), 0, 1);
                        var hardForce = Maths.Clamp(other.Radius == fix.Zero ? 0 : Maths.InvLerp(other.Radius, 0, distance), 0, 1);
                        var force = Maths.Max(hardForce, softForce * softForce) * other.SeparatorCoefficient * dir;
                        totalForce += force;
                        forcesCount++;
                    }

                    if (forcesCount > 0)
                    {
                        var offset = separation.Value / forcesCount * totalForce;
                        position += offset.x_y;
                    }
                }).Invoke(ref state);
        }

        private static int2 GetQuantizedSquare(fix2 position)
        {
            return new int2(
                (int)position.x / SquareSize,
                (int)position.y / SquareSize);
        }

        internal readonly struct SeparatorEntry
        {
            public readonly SyncId SyncId;
            public readonly fix2 Position;
            public readonly fix SeparatorRadius;
            public readonly fix SeparatorCoefficient;
            public readonly fix Radius;

            public SeparatorEntry(SyncId syncId, Position position, Separator separator, Radius radius)
            {
                SyncId = syncId;
                Position = position.Value.xz;
                SeparatorRadius = separator.Radius;
                SeparatorCoefficient = separator.Coefficient;
                Radius = radius;
            }
        }
    }
}
