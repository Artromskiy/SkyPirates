using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Delta.ECS;
using DVG.Components;
using DVG.SkyPirates.Shared.Data;

namespace DVG.SkyPirates.Shared.Ecs
{
    /// <summary>Stores sparse component changes reported by DeltaECS instead of full component frames.</summary>
    public sealed class ChangeWorldHistory : IWorldHistory
    {
        private const int NotRetired = int.MaxValue;

        private readonly World _world;
        private readonly ComponentId[] _componentIds;
        private readonly ComponentId _syncIdId;
        private readonly int _initialCapacity;
        private readonly int _maxCapacity;
        private readonly EntityValueMap<RetirementHistory> _retirements = new();
        private ComponentHistory[] _components = Array.Empty<ComponentHistory>();
        private ComponentHistory<SyncId> _syncIdHistory = null!;

        public ChangeWorldHistory(World world, ReadOnlySpan<ComponentId> componentIds, int initialCapacity, int maxCapacity)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            ValidateCapacity(initialCapacity, nameof(initialCapacity));
            ValidateCapacity(maxCapacity, nameof(maxCapacity));
            if (initialCapacity > maxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Maximum history capacity must be at least the initial capacity.");
            }

            _world = world;
            _syncIdId = world.Components.GetPrimary<SyncId>();
            _initialCapacity = initialCapacity;
            _maxCapacity = maxCapacity;
            _componentIds = CopyComponentIds(componentIds, _syncIdId);
            ResetComponentHistories();
        }

        public void Save(int tick) => Capture(tick, false);

        public void SaveBaseline()
        {
            ResetComponentHistories();
            for (int i = 0; i < _retirements.Count; i++)
            {
                RetirementHistory retirement = _retirements.GetValueAt(i);
                retirement.Values.Rollback(int.MinValue);
                retirement.Values[int.MinValue] = retirement.Current;
            }

            Capture(int.MinValue, true);
        }

        public void GoTo(int tick)
        {
            for (int i = 0; i < _retirements.Count; i++)
            {
                RetirementHistory retirement = _retirements.GetValueAt(i);
                retirement.Restore(retirement.Values[tick]);
            }

            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].GoTo(tick);
            }
        }

        public void Rollback(int tick)
        {
            GoTo(tick);
            for (int i = 0; i < _retirements.Count; i++)
            {
                RetirementHistory retirement = _retirements.GetValueAt(i);
                retirement.Values.Rollback(tick);
            }

            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].Rollback(tick);
            }
        }

        public WorldData GetSnapshot(int tick)
        {
            var snapshot = new WorldData();
            EntityValueMap<int> syncIds = _syncIdHistory.GetSyncIdsAt(tick);
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].PackSnapshot(snapshot, tick, syncIds);
            }

            return snapshot;
        }

        public void ApplySnapshot(WorldData snapshot, IReadOnlyDictionary<int, Entity> entitiesBySyncId)
        {
            foreach (Entity entity in entitiesBySyncId.Values)
            {
                for (int i = 0; i < _components.Length; i++)
                {
                    _components[i].RemoveFrom(entity);
                }
            }

            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].ApplySnapshot(snapshot, entitiesBySyncId);
            }
        }

        public void RetireEntityFromSnapshot(Entity entity)
        {
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].RemoveFrom(entity);
            }

            RetirementHistory retirement = GetOrCreateRetirement(entity);
            retirement.RetireFromSnapshot();
        }

        public void MarkForDisposal(Entity entity, int tick)
        {
            RetirementHistory retirement = GetOrCreateRetirement(entity);
            retirement.MarkForDisposal(tick);
        }

        public void CollectExpiredEntities(int tick, List<Entity> entities)
        {
            for (int i = 0; i < _retirements.Count; i++)
            {
                if (_retirements.GetValueAt(i).IsExpired(tick))
                {
                    entities.Add(_retirements.GetEntityAt(i));
                }
            }
        }

        public void DisposeEntity(Entity entity)
        {
            RemoveRetirement(entity);
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].Forget(entity);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _retirements.Count; i++)
            {
                _retirements.GetValueAt(i).Dispose();
            }

            _retirements.Clear();
            ResetComponentHistories();
        }

        public void Dispose()
        {
            for (int i = 0; i < _retirements.Count; i++)
            {
                _retirements.GetValueAt(i).Dispose();
            }

            _retirements.Clear();
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].Dispose();
            }

            _components = Array.Empty<ComponentHistory>();
        }

        private void Capture(int tick, bool baseline)
        {
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].BeginCapture(tick);
            }

            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].CaptureChanges();
            }

            CompleteRetirements(tick, baseline);
        }

        private void CompleteRetirements(int tick, bool baseline)
        {
            List<Entity> stale = null;
            for (int i = 0; i < _retirements.Count; i++)
            {
                Entity entity = _retirements.GetEntityAt(i);
                RetirementHistory retirement = _retirements.GetValueAt(i);
                if (!_world.Has(entity, _syncIdId))
                {
                    (stale ??= new List<Entity>()).Add(entity);
                    continue;
                }

                if (!baseline && retirement.RetirementPending)
                {
                    retirement.CompleteRetirement(tick);
                }

                retirement.Record(tick);
            }

            if (stale == null)
            {
                return;
            }

            for (int i = 0; i < stale.Count; i++)
            {
                RemoveRetirement(stale[i]);
            }
        }

        private void ResetComponentHistories()
        {
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].Dispose();
            }

            var histories = new List<ComponentHistory>(_componentIds.Length);
            var visitor = new CreateHistoryVisitor(this, histories);
            for (int i = 0; i < _componentIds.Length; i++)
            {
                _world.Components.Visit(_componentIds[i], visitor);
            }

            _components = histories.ToArray();
            for (int i = 0; i < _components.Length; i++)
            {
                if (_components[i].ComponentId == _syncIdId)
                {
                    _syncIdHistory = (ComponentHistory<SyncId>)_components[i];
                    return;
                }
            }
        }

        private RetirementHistory GetOrCreateRetirement(Entity entity)
        {
            if (_retirements.TryGetValue(entity, out RetirementHistory retirement))
            {
                return retirement;
            }

            retirement = new RetirementHistory(_initialCapacity, _maxCapacity);
            _retirements.Set(entity, in retirement);
            return retirement;
        }

        private void RemoveRetirement(Entity entity)
        {
            if (_retirements.TryGetValue(entity, out RetirementHistory retirement))
            {
                retirement.Dispose();
                _retirements.Remove(entity);
            }
        }

        private static ComponentId[] CopyComponentIds(ReadOnlySpan<ComponentId> componentIds, ComponentId syncIdId)
        {
            var result = new List<ComponentId>(componentIds.Length + 1);
            bool hasSyncId = false;
            for (int i = 0; i < componentIds.Length; i++)
            {
                ComponentId componentId = componentIds[i];
                bool duplicate = false;
                for (int j = 0; j < result.Count; j++)
                {
                    if (result[j] == componentId)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                {
                    result.Add(componentId);
                }

                hasSyncId |= componentId == syncIdId;
            }

            if (!hasSyncId)
            {
                result.Add(syncIdId);
            }

            return result.ToArray();
        }

        private static void ValidateCapacity(int capacity, string name)
        {
            if (capacity <= 0 || (capacity & (capacity - 1)) != 0)
            {
                throw new ArgumentOutOfRangeException(name, "History capacity must be a positive power of two.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool SameEntity(Entity left, Entity right)
            => left.Index == right.Index && left.Generation == right.Generation;

        private sealed class CreateHistoryVisitor : IStructVisitor
        {
            private readonly ChangeWorldHistory _owner;
            private readonly List<ComponentHistory> _histories;

            public CreateHistoryVisitor(ChangeWorldHistory owner, List<ComponentHistory> histories)
            {
                _owner = owner;
                _histories = histories;
            }

            public void Visit<T>(ComponentId componentId) where T : struct
                => _histories.Add(new ComponentHistory<T>(_owner, componentId, _owner._initialCapacity, _owner._maxCapacity));
        }

        internal struct ComponentRestoreState<T> where T : struct
        {
            public readonly T[] ValuesByEntityIndex;
            public readonly Entity[] EntitiesByEntityIndex;
            public readonly int[] RestoreStampsByEntityIndex;
            public readonly int[] SeenStampsByEntityIndex;
            public readonly int RestoreStamp;
            public readonly List<Entity> EntitiesToRemove;

            public ComponentRestoreState(
                T[] valuesByEntityIndex,
                Entity[] entitiesByEntityIndex,
                int[] restoreStampsByEntityIndex,
                int[] seenStampsByEntityIndex,
                int restoreStamp,
                List<Entity> entitiesToRemove)
            {
                ValuesByEntityIndex = valuesByEntityIndex;
                EntitiesByEntityIndex = entitiesByEntityIndex;
                RestoreStampsByEntityIndex = restoreStampsByEntityIndex;
                SeenStampsByEntityIndex = seenStampsByEntityIndex;
                RestoreStamp = restoreStamp;
                EntitiesToRemove = entitiesToRemove;
            }
        }

        internal struct RestoreExistingComponent<T> : IForEachContextEntity<ComponentRestoreState<T>> where T : struct
        {
            public void Invoke(ref ComponentRestoreState<T> state, EntityRef entity, ref T component)
            {
                Entity handle = entity;
                int index = handle.Index;
                if ((uint)index < (uint)state.RestoreStampsByEntityIndex.Length
                    && state.RestoreStampsByEntityIndex[index] == state.RestoreStamp
                    && SameEntity(state.EntitiesByEntityIndex[index], handle))
                {
                    component = state.ValuesByEntityIndex[index];
                    state.SeenStampsByEntityIndex[index] = state.RestoreStamp;
                }
                else
                {
                    state.EntitiesToRemove.Add(handle);
                }
            }
        }

        private abstract class ComponentHistory : IDisposable
        {
            protected ComponentHistory(ComponentId componentId)
            {
                ComponentId = componentId;
            }

            public ComponentId ComponentId { get; }
            public abstract void BeginCapture(int tick);
            public abstract void CaptureChanges();
            public abstract void GoTo(int tick);
            public abstract void Rollback(int tick);
            public abstract void PackSnapshot(WorldData snapshot, int tick, EntityValueMap<int> syncIds);
            public abstract void ApplySnapshot(WorldData snapshot, IReadOnlyDictionary<int, Entity> entitiesBySyncId);
            public abstract void RemoveFrom(Entity entity);
            public abstract void Forget(Entity entity);
            public abstract void Dispose();
        }

        private sealed class ComponentHistory<T> : ComponentHistory where T : struct
        {
            private readonly ChangeWorldHistory _owner;
            private readonly ComponentId _componentId;
            private readonly ComponentChangeOperation<T> _operation;
            private readonly Query _restoreQuery;
            private readonly IOperation<ComponentRestoreState<T>> _restoreOperation;
            private readonly ChangeFrameHistory<T> _frames;
            private readonly List<Entity> _entitiesToRemove = new();
            private readonly List<Entity> _restoredEntities = new();
            private T[] _restoreValuesByEntityIndex = Array.Empty<T>();
            private Entity[] _restoreEntitiesByEntityIndex = Array.Empty<Entity>();
            private int[] _restoreStampByEntityIndex = Array.Empty<int>();
            private int[] _seenRestoreStampByEntityIndex = Array.Empty<int>();
            private int _restoreStamp;
            private ChangeFrame<T> _captureFrame = null!;

            public ComponentHistory(ChangeWorldHistory owner, ComponentId componentId, int initialCapacity, int maxCapacity)
                : base(componentId)
            {
                _owner = owner;
                _componentId = componentId;
                _frames = new ChangeFrameHistory<T>(initialCapacity, maxCapacity);
                _operation = owner._world.ForEachChange<T>(componentId, OnChange);
                _restoreQuery = owner._world.WhereAll(componentId);
                var initialRestoreState = default(ComponentRestoreState<T>);
                _restoreOperation = owner._world.ForEachEntity(
                    in _restoreQuery,
                    ref initialRestoreState,
                    componentId,
                    typeof(RestoreExistingComponent<>));
            }

            public override void BeginCapture(int tick) => _captureFrame = _frames.BeginWrite(tick);

            public override void CaptureChanges() => _operation.Invoke();

            public override void GoTo(int tick) => RestoreFullState(tick);

            private void RestoreFullState(int tick)
            {
                EntityValueMap<T> state = _frames.GetState(tick);
                MarkRestoredEntities(state);
                _entitiesToRemove.Clear();
                var restoreState = new ComponentRestoreState<T>(
                    _restoreValuesByEntityIndex,
                    _restoreEntitiesByEntityIndex,
                    _restoreStampByEntityIndex,
                    _seenRestoreStampByEntityIndex,
                    _restoreStamp,
                    _entitiesToRemove);
                _restoreOperation.Invoke(ref restoreState);

                for (int i = 0; i < _entitiesToRemove.Count; i++)
                {
                    _owner._world.Remove(_entitiesToRemove[i], _componentId);
                }

                for (int i = 0; i < _restoredEntities.Count; i++)
                {
                    Entity entity = _restoredEntities[i];
                    int index = entity.Index;
                    if (_seenRestoreStampByEntityIndex[index] != _restoreStamp
                        && _owner._world.IsAlive(entity))
                    {
                        T value = _restoreValuesByEntityIndex[index];
                        _owner._world.Add(entity, in value);
                    }
                }
            }

            private void MarkRestoredEntities(EntityValueMap<T> state)
            {
                if (_restoreStamp == int.MaxValue)
                {
                    Array.Clear(_restoreStampByEntityIndex, 0, _restoreStampByEntityIndex.Length);
                    Array.Clear(_seenRestoreStampByEntityIndex, 0, _seenRestoreStampByEntityIndex.Length);
                    _restoreStamp = 1;
                }
                else
                {
                    _restoreStamp++;
                }

                _restoredEntities.Clear();
                for (int i = 0; i < state.Count; i++)
                {
                    Entity entity = state.GetEntityAt(i);
                    int index = entity.Index;
                    EnsureRestoreCapacity(index);
                    _restoredEntities.Add(entity);
                    _restoreEntitiesByEntityIndex[index] = entity;
                    _restoreValuesByEntityIndex[index] = state.GetValueAt(i);
                    _restoreStampByEntityIndex[index] = _restoreStamp;
                }
            }

            private void EnsureRestoreCapacity(int entityIndex)
            {
                if (entityIndex < _restoreStampByEntityIndex.Length)
                {
                    return;
                }

                int capacity = _restoreStampByEntityIndex.Length == 0 ? 4 : _restoreStampByEntityIndex.Length;
                while (capacity <= entityIndex)
                {
                    capacity *= 2;
                }

                Array.Resize(ref _restoreValuesByEntityIndex, capacity);
                Array.Resize(ref _restoreEntitiesByEntityIndex, capacity);
                Array.Resize(ref _restoreStampByEntityIndex, capacity);
                Array.Resize(ref _seenRestoreStampByEntityIndex, capacity);
            }

            public override void Rollback(int tick) => _frames.Rollback(tick);

            public override void PackSnapshot(WorldData snapshot, int tick, EntityValueMap<int> syncIds)
            {
                EntityValueMap<T> state = _frames.GetState(tick);
                Dictionary<int, T> output = snapshot.Get<T>();
                for (int i = 0; i < state.Count; i++)
                {
                    Entity entity = state.GetEntityAt(i);
                    if (syncIds.TryGetValue(entity, out int syncId))
                    {
                        output[syncId] = state.GetValueAt(i);
                    }
                }
            }

            public override void ApplySnapshot(WorldData snapshot, IReadOnlyDictionary<int, Entity> entitiesBySyncId)
            {
                Dictionary<int, T> components = snapshot.Get<T>();
                foreach (var pair in components)
                {
                    if (!entitiesBySyncId.TryGetValue(pair.Key, out Entity entity))
                    {
                        continue;
                    }

                    T value = pair.Value;
                    if (_owner._world.Has(entity, _componentId))
                    {
                        _owner._world.GetRef<T>(entity, _componentId) = value;
                    }
                    else
                    {
                        _owner._world.Add(entity, in value);
                    }
                }
            }

            public override void RemoveFrom(Entity entity)
            {
                if (_owner._world.Has(entity, _componentId))
                {
                    _owner._world.Remove(entity, _componentId);
                }
            }

            public override void Forget(Entity entity)
            {
                _frames.Forget(entity);
            }

            public override void Dispose() => _frames.Dispose();

            private void OnChange(Entity entity, ChangeKind change, ref T component)
            {
                bool hasNewValue = change != ChangeKind.Removed;
                T newValue = hasNewValue ? component : default;
                _frames.Add(_captureFrame, entity, hasNewValue, in newValue);
                if (!hasNewValue && _componentId == _owner._syncIdId)
                {
                    _owner.RemoveRetirement(entity);
                }
            }

            public EntityValueMap<int> GetSyncIdsAt(int tick)
            {
                EntityValueMap<T> state = _frames.GetState(tick);
                var result = new EntityValueMap<int>();
                for (int i = 0; i < state.Count; i++)
                {
                    Entity entity = state.GetEntityAt(i);
                    T value = state.GetValueAt(i);
                    SyncId syncId = Unsafe.As<T, SyncId>(ref value);
                    int id = syncId.Value;
                    result.Set(entity, in id);
                }

                return result;
            }
        }

        private sealed class ChangeFrameHistory<T> : IDisposable where T : struct
        {
            private ChangeFrame<T>[] _frames;
            private readonly EntityValueMap<T> _baseline = new();
            private readonly EntityValueMap<T> _stateCache = new();
            private readonly EntityValueMap<int> _captureIndices = new();
            private int _mask;
            private int _head;
            private int _count;
            private int _cachedStateTick;
            private bool _hasCachedState;
            private readonly int _maxCapacity;

            public ChangeFrameHistory(int initialCapacity, int maxCapacity)
            {
                _frames = new ChangeFrame<T>[initialCapacity];
                _mask = initialCapacity - 1;
                _maxCapacity = maxCapacity;
                _frames[0] = new ChangeFrame<T>(int.MinValue);
                _count = 1;
            }

            public ChangeFrame<T> BeginWrite(int tick)
            {
                if (tick < Last.Tick)
                {
                    Rollback(tick);
                }

                if (_hasCachedState && tick <= _cachedStateTick)
                {
                    _hasCachedState = false;
                }

                if (Last.Tick == tick)
                {
                    return Last;
                }

                _captureIndices.Clear();
                EnsureCapacity();
                int index = (_head + _count) & _mask;
                ChangeFrame<T> frame = _frames[index] ?? (_frames[index] = new ChangeFrame<T>(tick));
                frame.Reset(tick);
                _count++;
                return frame;
            }

            public void Add(
                ChangeFrame<T> frame,
                Entity entity,
                bool hasNewValue,
                in T newValue)
                => frame.Add(entity, hasNewValue, in newValue, _captureIndices);

            public EntityValueMap<T> GetState(int tick)
            {
                if (tick < First.Tick)
                {
                    throw new IndexOutOfRangeException($"History does not contain tick {tick}");
                }

                if (_hasCachedState && _cachedStateTick == tick)
                {
                    return _stateCache;
                }

                if (!_hasCachedState || tick < _cachedStateTick)
                {
                    _stateCache.Clear();
                    for (int i = 0; i < _baseline.Count; i++)
                    {
                        Entity entity = _baseline.GetEntityAt(i);
                        T value = _baseline.GetValueAt(i);
                        _stateCache.Set(entity, in value);
                    }

                    for (int i = 0; i < _count; i++)
                    {
                        ChangeFrame<T> frame = At(i);
                        if (frame.Tick > tick)
                        {
                            break;
                        }

                        frame.ApplyTo(_stateCache);
                    }
                }
                else
                {
                    for (int i = 0; i < _count; i++)
                    {
                        ChangeFrame<T> frame = At(i);
                        if (frame.Tick <= _cachedStateTick)
                        {
                            continue;
                        }

                        if (frame.Tick > tick)
                        {
                            break;
                        }

                        frame.ApplyTo(_stateCache);
                    }
                }

                _cachedStateTick = tick;
                _hasCachedState = true;
                return _stateCache;
            }

            public void Rollback(int tick)
            {
                if (_hasCachedState && tick < _cachedStateTick)
                {
                    _hasCachedState = false;
                }

                if (_count == 0 || tick < First.Tick)
                {
                    throw new IndexOutOfRangeException($"History does not contain tick {tick}");
                }

                while (_count > 1 && Last.Tick > tick)
                {
                    Last.Clear();
                    _count--;
                }

                RebuildCaptureIndices();
            }

            public void Forget(Entity entity)
            {
                _hasCachedState = false;
                _baseline.Remove(entity);
                bool lastFrameChanged = false;
                for (int i = 0; i < _count; i++)
                {
                    bool changed = At(i).Forget(entity);
                    if (i == _count - 1)
                    {
                        lastFrameChanged = changed;
                    }
                }

                if (lastFrameChanged)
                {
                    RebuildCaptureIndices();
                }
            }

            public void Dispose()
            {
                _hasCachedState = false;
                _stateCache.Clear();
                _captureIndices.Clear();
                for (int i = 0; i < _frames.Length; i++)
                {
                    _frames[i]?.Clear();
                    _frames[i] = null;
                }

                _baseline.Clear();
                _count = 0;
            }

            private void EnsureCapacity()
            {
                if (_count < _frames.Length)
                {
                    return;
                }

                if (_frames.Length < _maxCapacity)
                {
                    int capacity = Math.Min(_frames.Length << 1, _maxCapacity);
                    var expanded = new ChangeFrame<T>[capacity];
                    for (int i = 0; i < _count; i++)
                    {
                        expanded[i] = At(i);
                    }

                    _frames = expanded;
                    _head = 0;
                    _mask = capacity - 1;
                    return;
                }

                ChangeFrame<T> expired = First;
                expired.ApplyTo(_baseline);
                expired.Clear();
                _head = (_head + 1) & _mask;
                _count--;
            }

            private ChangeFrame<T> First => At(0);
            private ChangeFrame<T> Last => At(_count - 1);
            private ChangeFrame<T> At(int logicalIndex) => _frames[(_head + logicalIndex) & _mask];

            private void RebuildCaptureIndices()
            {
                _captureIndices.Clear();
                ChangeFrame<T> last = Last;
                for (int i = 0; i < last.Count; i++)
                {
                    int index = i;
                    _captureIndices.Set(last.GetEntity(i), in index);
                }
            }
        }

        private sealed class ChangeFrame<T> where T : struct
        {
            private readonly List<Change<T>> _changes = new();

            public int Tick { get; private set; }
            public int Count => _changes.Count;

            public ChangeFrame(int tick)
            {
                Tick = tick;
            }

            public void Reset(int tick)
            {
                _changes.Clear();
                Tick = tick;
            }

            public void Add(
                Entity entity,
                bool hasNewValue,
                in T newValue,
                EntityValueMap<int> indices)
            {
                if (indices.TryGetValue(entity, out int index))
                {
                    _changes[index] = _changes[index].WithNewValue(hasNewValue, in newValue);
                    return;
                }

                int nextIndex = _changes.Count;
                indices.Set(entity, in nextIndex);
                _changes.Add(new Change<T>(entity, hasNewValue, in newValue));
            }

            public Entity GetEntity(int index) => _changes[index].Entity;

            public void ApplyTo(EntityValueMap<T> state)
            {
                for (int i = 0; i < _changes.Count; i++)
                {
                    Change<T> change = _changes[i];
                    if (!change.HasNewValue)
                    {
                        state.Remove(change.Entity);
                    }
                    else
                    {
                        state.Set(change.Entity, in change.NewValue);
                    }
                }
            }

            public bool Forget(Entity entity)
            {
                bool removed = false;
                for (int i = _changes.Count - 1; i >= 0; i--)
                {
                    if (SameEntity(_changes[i].Entity, entity))
                    {
                        _changes.RemoveAt(i);
                        removed = true;
                    }
                }

                return removed;
            }

            public void Clear() => _changes.Clear();
        }

        private sealed class EntityValueMap<T>
        {
            private const int PageShift = 8;
            private const int PageSize = 1 << PageShift;
            private const int PageMask = PageSize - 1;
            private const int DirectoryShift = 8;
            private const int DirectorySize = 1 << DirectoryShift;
            private const int DirectoryMask = DirectorySize - 1;
            private const int RootSize = 1 << (31 - PageShift - DirectoryShift * 2);

            private MiddleDirectory?[] _root = Array.Empty<MiddleDirectory?>();
            private readonly List<Entity> _entities = new();

            public int Count => _entities.Count;

            public Entity GetEntityAt(int index) => _entities[index];

            public T GetValueAt(int index)
            {
                Entity entity = _entities[index];
                Page page = GetPage(entity.Index, false)!;
                return page.Values[entity.Index & PageMask];
            }

            public bool TryGetValue(Entity entity, out T value)
            {
                Page? page = GetPage(entity.Index, false);
                int slot = entity.Index & PageMask;
                if (page != null && page.Present[slot] && SameEntity(page.Entities[slot], entity))
                {
                    value = page.Values[slot];
                    return true;
                }

                value = default!;
                return false;
            }

            public void Set(Entity entity, in T value)
            {
                Page page = GetPage(entity.Index, true)!;
                int slot = entity.Index & PageMask;
                if (page.Present[slot])
                {
                    if (SameEntity(page.Entities[slot], entity))
                    {
                        page.Values[slot] = value;
                        return;
                    }

                    RemoveAt(page, slot);
                }

                page.Entities[slot] = entity;
                page.Values[slot] = value;
                page.DenseIndices[slot] = _entities.Count;
                page.Present[slot] = true;
                _entities.Add(entity);
            }

            public bool Remove(Entity entity)
            {
                Page? page = GetPage(entity.Index, false);
                int slot = entity.Index & PageMask;
                if (page == null || !page.Present[slot] || !SameEntity(page.Entities[slot], entity))
                {
                    return false;
                }

                RemoveAt(page, slot);
                return true;
            }

            public void Clear()
            {
                for (int i = 0; i < _entities.Count; i++)
                {
                    Entity entity = _entities[i];
                    Page page = GetPage(entity.Index, false)!;
                    int slot = entity.Index & PageMask;
                    page.Entities[slot] = default;
                    page.Values[slot] = default!;
                    page.DenseIndices[slot] = 0;
                    page.Present[slot] = false;
                }

                _entities.Clear();
            }

            private void RemoveAt(Page page, int slot)
            {
                int removeIndex = page.DenseIndices[slot];
                int lastIndex = _entities.Count - 1;
                if (removeIndex != lastIndex)
                {
                    Entity moved = _entities[lastIndex];
                    _entities[removeIndex] = moved;
                    Page movedPage = GetPage(moved.Index, false)!;
                    movedPage.DenseIndices[moved.Index & PageMask] = removeIndex;
                }

                _entities.RemoveAt(lastIndex);
                page.Entities[slot] = default;
                page.Values[slot] = default!;
                page.DenseIndices[slot] = 0;
                page.Present[slot] = false;
            }

            private Page? GetPage(int entityIndex, bool create)
            {
                if (entityIndex < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(entityIndex));
                }

                int pageIndex = entityIndex >> PageShift;
                int rootIndex = pageIndex >> (DirectoryShift * 2);
                int middleIndex = (pageIndex >> DirectoryShift) & DirectoryMask;
                int leafIndex = pageIndex & DirectoryMask;

                if (_root.Length == 0)
                {
                    if (!create)
                    {
                        return null;
                    }

                    _root = new MiddleDirectory?[RootSize];
                }

                MiddleDirectory? middle = _root[rootIndex];
                if (middle == null)
                {
                    if (!create)
                    {
                        return null;
                    }

                    middle = new MiddleDirectory();
                    _root[rootIndex] = middle;
                }

                LeafDirectory? leaf = middle.Children[middleIndex];
                if (leaf == null)
                {
                    if (!create)
                    {
                        return null;
                    }

                    leaf = new LeafDirectory();
                    middle.Children[middleIndex] = leaf;
                }

                Page? page = leaf.Pages[leafIndex];
                if (page == null && create)
                {
                    page = new Page();
                    leaf.Pages[leafIndex] = page;
                }

                return page;
            }

            private sealed class MiddleDirectory
            {
                public readonly LeafDirectory?[] Children = new LeafDirectory?[DirectorySize];
            }

            private sealed class LeafDirectory
            {
                public readonly Page?[] Pages = new Page?[DirectorySize];
            }

            private sealed class Page
            {
                public readonly Entity[] Entities = new Entity[PageSize];
                public readonly T[] Values = new T[PageSize];
                public readonly int[] DenseIndices = new int[PageSize];
                public readonly bool[] Present = new bool[PageSize];
            }
        }

        private readonly struct Change<T> where T : struct
        {
            public readonly Entity Entity;
            public readonly bool HasNewValue;
            public readonly T NewValue;

            public Change(Entity entity, bool hasNewValue, in T newValue)
            {
                Entity = entity;
                HasNewValue = hasNewValue;
                NewValue = newValue;
            }

            public Change<T> WithNewValue(bool hasNewValue, in T newValue)
                => new(Entity, hasNewValue, in newValue);
        }

        private sealed class RetirementHistory : IDisposable
        {
            public History<RetirementState> Values;
            public int RetiredAt = NotRetired;
            public bool RetirementPending;

            public RetirementHistory(int initialCapacity, int maxCapacity)
            {
                Values = new History<RetirementState>(initialCapacity, maxCapacity);
            }

            public RetirementState Current => new(RetiredAt, RetirementPending);

            public void RetireFromSnapshot()
            {
                RetiredAt = NotRetired;
                RetirementPending = true;
            }

            public void MarkForDisposal(int tick)
            {
                RetiredAt = tick;
                RetirementPending = false;
                Values[tick] = Current;
            }

            public void CompleteRetirement(int tick)
            {
                RetiredAt = tick;
                RetirementPending = false;
            }

            public void Record(int tick) => Values[tick] = Current;

            public void Restore(RetirementState? state)
            {
                RetiredAt = state.HasValue ? state.Value.RetiredAt : NotRetired;
                RetirementPending = state.HasValue && state.Value.RetirementPending;
            }

            public bool IsExpired(int tick)
            {
                RetirementState? state = Values.GetLast(out _);
                return state.HasValue
                    && !state.Value.RetirementPending
                    && state.Value.RetiredAt != NotRetired
                    && state.Value.RetiredAt <= tick - Constants.MaxHistoryTicks;
            }

            public void Dispose() => Values.Dispose();
        }

        private readonly struct RetirementState
        {
            public readonly int RetiredAt;
            public readonly bool RetirementPending;

            public RetirementState(int retiredAt, bool retirementPending)
            {
                RetiredAt = retiredAt;
                RetirementPending = retirementPending;
            }
        }
    }
}
