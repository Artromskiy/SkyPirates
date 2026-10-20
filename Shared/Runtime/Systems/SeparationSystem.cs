using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Core.Collections;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace SlyPirates.Shared.Systems
{
    public sealed class SeparationSystem : IDeltaTickableExecutor
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
        private readonly List<List<SeparatorEntry>> _partitionLists = new();
        private readonly HashSet<int> _syncIdCache = new();
        private PartitionBuildState _partitionState;
        private readonly IOperation<PartitionBuildState> _buildPartitioning;
        private (HashSet<int> Written, Lookup2D<List<SeparatorEntry>> Grid) _separationState;
        private readonly IOperation<(HashSet<int> Written, Lookup2D<List<SeparatorEntry>> Grid)> _applySeparation;

        private readonly World _world;

        public SeparationSystem(World world)
        {
            _world = world;
            _partitionState = new PartitionBuildState(_partitioning, _partitionLists);
            var separatorDesc = _separatorDesc;
            _buildPartitioning = world.ForEach<PartitionBuildState, SyncId, Position, Separator, Radius>(in separatorDesc, ref _partitionState,
                static (ref PartitionBuildState state, in SyncId syncId, in Position position, in Separator separator, in Radius radius) =>
                {
                    var pos = ((fix3)position).xz;
                    var range = new fix2(radius.Value);
                    var minQ = GetQuantizedSquare(pos - range);
                    var maxQ = GetQuantizedSquare(pos + range);

                    for (int y = minQ.y; y <= maxQ.y; y++)
                    for (int x = minQ.x; x <= maxQ.x; x++)
                    {
                        if (!state.Grid.TryGetValue(x, y, out var list))
                        {
                            if (state.NextList == state.Buffers.Count)
                            {
                                state.Buffers.Add(new List<SeparatorEntry>(8));
                            }

                            list = state.Buffers[state.NextList++];
                            list.Clear();
                            state.Grid[x, y] = list;
                        }

                        list.Add(new SeparatorEntry(syncId, position, separator, radius.Value));
                    }
                });

            _separationState = (_syncIdCache, _partitioning);
            var separationDesc = _separationDesc;
            _applySeparation = world.ForEach<(HashSet<int> Written, Lookup2D<List<SeparatorEntry>> Grid), Position, Separation>(in separationDesc, ref _separationState,
                static (ref (HashSet<int> Written, Lookup2D<List<SeparatorEntry>> Grid) state, ref Position position, in Separation separation) =>
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
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            for (int i = 0; i < _partitionState.NextList; i++)
            {
                _partitionLists[i].Clear();
            }

            _partitioning.Clear();
            _partitionState.NextList = 0;
            _buildPartitioning.Invoke(ref _partitionState);
            if (_partitionState.NextList != 0)
            {
                _applySeparation.Invoke(ref _separationState);
            }
        }

        private struct PartitionBuildState
        {
            public readonly Lookup2D<List<SeparatorEntry>> Grid;
            public readonly List<List<SeparatorEntry>> Buffers;
            public int NextList;

            public PartitionBuildState(Lookup2D<List<SeparatorEntry>> grid, List<List<SeparatorEntry>> buffers)
            {
                Grid = grid;
                Buffers = buffers;
                NextList = 0;
            }
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
