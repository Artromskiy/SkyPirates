using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Delta.ECS;
using DVG.SkyPirates.Shared.Components;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Data;

namespace DVG.SkyPirates.Shared.Ecs
{
    /// <summary>Stores component history outside the ECS world.</summary>
    public sealed class WorldHistory : IWorldHistory
    {
        private readonly ComponentHistory[] _components;
        private readonly ComponentHistory<SyncId> _syncIdHistory;
        private readonly EntityPresenceHistory _entityPresence;

        public WorldHistory(World world, ReadOnlySpan<ComponentId> componentIds, int initialCapacity, int maxCapacity)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (initialCapacity <= 0 || (initialCapacity & (initialCapacity - 1)) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "History capacity must be a positive power of two.");
            }

            if (maxCapacity <= 0 || (maxCapacity & (maxCapacity - 1)) != 0 || initialCapacity > maxCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Maximum history capacity must be a power of two at least as large as the initial capacity.");
            }

            ComponentId syncId = world.Components.GetPrimary<SyncId>();
            _entityPresence = new EntityPresenceHistory(initialCapacity, maxCapacity);
            var histories = new List<ComponentHistory>(componentIds.Length);
            var visitor = new CreateHistoryVisitor(world, _entityPresence, histories, initialCapacity, maxCapacity);
            for (int i = 0; i < componentIds.Length; i++)
            {
                world.Components.Visit(componentIds[i], visitor);
            }

            _syncIdHistory = GetOrCreateHistory<SyncId>(world, _entityPresence, histories, syncId, initialCapacity, maxCapacity);
            _components = histories.ToArray();
        }

        public void Save(int tick) => CaptureFrame(tick, false);

        private void CaptureFrame(int tick, bool baseline)
        {
            _entityPresence.BeginCapture(tick, baseline);
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].PrepareCaptureFrame(tick);
            }

            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].CapturePreparedFrame();
            }
            _entityPresence.CompleteCapture(tick, baseline);
        }

        public void SaveBaseline()
        {
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].ResetBaseline();
            }

            CaptureFrame(int.MinValue, true);
        }

        public void GoTo(int tick)
        {
            _entityPresence.GoTo(tick);
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].GoTo(tick);
            }
        }

        public void Rollback(int tick)
        {
            GoTo(tick);
            _entityPresence.Rollback(tick);
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].Rollback(tick);
            }
        }

        public WorldData GetSnapshot(int tick)
        {
            var snapshot = new WorldData();
            Dictionary<Entity, int> syncIds = _syncIdHistory.GetSyncIdsAt(tick);
            EntityPresenceFrame entities = _entityPresence.GetFrame(tick);
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].PackSnapshot(snapshot, tick, entities, syncIds);
            }

            return snapshot;
        }

        public void ApplySnapshot(WorldData snapshot, IReadOnlyDictionary<int, Entity> entitiesBySyncId)
        {
            foreach (var entity in entitiesBySyncId.Values)
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

            _entityPresence.RetireFromSnapshot(entity);
        }

        // Keeps retired entities in snapshots until their rollback window expires.
        public void MarkForDisposal(Entity entity, int tick) => _entityPresence.MarkForDisposal(entity, tick);

        public void CollectExpiredEntities(int tick, List<Entity> entities) =>
            _entityPresence.CollectExpiredEntities(tick, entities);

        public void DisposeEntity(Entity entity)
        {
            _entityPresence.DisposeEntity(entity);
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].DisposeEntity(entity);
            }
        }

        public void Clear()
        {
            _entityPresence.Clear();
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].Clear();
            }
        }

        public void Dispose()
        {
            _entityPresence.Dispose();
            for (int i = 0; i < _components.Length; i++)
            {
                _components[i].Dispose();
            }
        }

        private static ComponentHistory<T> GetOrCreateHistory<T>(
            World world,
            EntityPresenceHistory entityPresence,
            List<ComponentHistory> histories,
            ComponentId componentId,
            int initialCapacity,
            int maxCapacity) where T : struct
        {
            for (int i = 0; i < histories.Count; i++)
            {
                if (histories[i] is ComponentHistory<T> typed)
                {
                    return typed;
                }
            }

            var history = new ComponentHistory<T>(world, entityPresence, componentId, initialCapacity, maxCapacity);
            histories.Add(history);
            return history;
        }

        private sealed class CreateHistoryVisitor : IStructComponentVisitor
        {
            private readonly World _world;
            private readonly EntityPresenceHistory _entityPresence;
            private readonly List<ComponentHistory> _histories;
            private readonly int _initialCapacity;
            private readonly int _maxCapacity;

            public CreateHistoryVisitor(
                World world,
                EntityPresenceHistory entityPresence,
                List<ComponentHistory> histories,
                int initialCapacity,
                int maxCapacity)
            {
                _world = world;
                _entityPresence = entityPresence;
                _histories = histories;
                _initialCapacity = initialCapacity;
                _maxCapacity = maxCapacity;
            }

            public void Visit<T>(ComponentId componentId) where T : struct
                => _histories.Add(new ComponentHistory<T>(_world, _entityPresence, componentId, _initialCapacity, _maxCapacity));
        }

        private sealed class EntityPresenceHistory : IDisposable
        {
            private const int NotRetired = int.MaxValue;

            private readonly Dictionary<Entity, Entry> _retirements = new();
            private readonly List<Entity> _staleEntities = new();
            private readonly EntityPresenceFrameHistory _frames;
            private readonly int _initialCapacity;
            private readonly int _maxCapacity;
            private EntityPresenceFrame _captureFrame = null!;

            public EntityPresenceHistory(int initialCapacity, int maxCapacity)
            {
                _initialCapacity = initialCapacity;
                _maxCapacity = maxCapacity;
                _frames = new EntityPresenceFrameHistory(initialCapacity, maxCapacity);
            }

            public EntityPresenceFrame GetFrame(int tick) => _frames.Get(tick);

            public void BeginCapture(int tick, bool baseline)
            {
                if (baseline)
                {
                    foreach (var entry in _retirements.Values)
                    {
                        entry.Values.Rollback(int.MinValue);
                    }
                }

                _captureFrame = _frames.BeginWrite(tick);
            }

            public void Capture(Entity entity) => _captureFrame.Mark(entity);

            public void CompleteCapture(int tick, bool baseline)
            {
                EntityPresenceFrame frame = _captureFrame;
                _captureFrame = null!;

                _staleEntities.Clear();
                foreach (var pair in _retirements)
                {
                    Entry entry = pair.Value;
                    if (!frame.Contains(pair.Key))
                    {
                        _staleEntities.Add(pair.Key);
                        continue;
                    }

                    if (!baseline && entry.RetirementPending)
                    {
                        entry.RetiredAt = tick;
                        entry.RetirementPending = false;
                    }

                    entry.Values[tick] = new EntityState(true, entry.RetiredAt, entry.RetirementPending);
                }

                for (int i = 0; i < _staleEntities.Count; i++)
                {
                    DisposeEntity(_staleEntities[i]);
                }
            }

            public void MarkForDisposal(Entity entity, int tick)
            {
                Entry entry = GetOrCreate(entity);
                entry.RetiredAt = tick;
                entry.RetirementPending = false;
                entry.Values[tick] = new EntityState(true, tick, false);
            }

            public void RetireFromSnapshot(Entity entity)
            {
                Entry entry = GetOrCreate(entity);
                entry.RetiredAt = NotRetired;
                entry.RetirementPending = true;
            }

            public void CollectExpiredEntities(int tick, List<Entity> entities)
            {
                foreach (var pair in _retirements)
                {
                    EntityState? state = pair.Value.Values.GetLast(out _);
                    if (state.HasValue && !state.Value.RetirementPending
                                       && state.Value.RetiredAt != NotRetired
                                       && state.Value.RetiredAt <= tick - Constants.MaxHistoryTicks)
                    {
                        entities.Add(pair.Key);
                    }
                }
            }

            public void Rollback(int tick)
            {
                _frames.Rollback(tick);
                foreach (var entry in _retirements.Values)
                {
                    entry.Values.Rollback(tick);
                    RestoreCurrentState(entry, entry.Values.GetLast(out _));
                }
            }

            public void GoTo(int tick)
            {
                foreach (var entry in _retirements.Values)
                {
                    RestoreCurrentState(entry, entry.Values[tick]);
                }
            }

            public void DisposeEntity(Entity entity)
            {
                if (_retirements.TryGetValue(entity, out Entry entry))
                {
                    entry.Values.Dispose();
                    _retirements.Remove(entity);
                }
            }

            public void Clear()
            {
                foreach (var entry in _retirements.Values)
                {
                    entry.Values.Dispose();
                }

                _retirements.Clear();
                _frames.ResetBaseline();
                _captureFrame = null!;
            }

            public void Dispose()
            {
                Clear();
                _frames.Dispose();
            }

            private Entry GetOrCreate(Entity entity)
            {
                if (!_retirements.TryGetValue(entity, out Entry entry))
                {
                    entry = new Entry(_initialCapacity, _maxCapacity);
                    _retirements.Add(entity, entry);
                }

                return entry;
            }

            private static void RestoreCurrentState(Entry entry, EntityState? state)
            {
                entry.RetiredAt = state?.RetiredAt ?? NotRetired;
                entry.RetirementPending = state.HasValue && state.Value.RetirementPending;
            }

            private sealed class Entry
            {
                public History<EntityState> Values;
                public int RetiredAt = NotRetired;
                public bool RetirementPending;

                public Entry(int initialCapacity, int maxCapacity)
                {
                    Values = new History<EntityState>(initialCapacity, maxCapacity);
                }
            }

            private readonly struct EntityState
            {
                public readonly bool Exists;
                public readonly int RetiredAt;
                public readonly bool RetirementPending;

                public EntityState(bool exists, int retiredAt, bool retirementPending)
                {
                    Exists = exists;
                    RetiredAt = retiredAt;
                    RetirementPending = retirementPending;
                }
            }
        }

        private sealed class EntityPresenceFrame
        {
            private Entity[] _entitiesByIndex = Array.Empty<Entity>();
            private int[] _stampsByIndex = Array.Empty<int>();
            private int _stamp;

            public int Tick { get; private set; }

            public void Begin(int tick)
            {
                if (_stamp == int.MaxValue)
                {
                    Array.Clear(_stampsByIndex, 0, _stampsByIndex.Length);
                    _stamp = 1;
                }
                else
                {
                    _stamp++;
                }

                Tick = tick;
            }

            public void Mark(Entity entity)
            {
                EnsureCapacity(entity.Index);
                _entitiesByIndex[entity.Index] = entity;
                _stampsByIndex[entity.Index] = _stamp;
            }

            public bool Contains(Entity entity)
            {
                int index = entity.Index;
                return (uint)index < (uint)_stampsByIndex.Length
                    && _stampsByIndex[index] == _stamp
                    && _entitiesByIndex[index] == entity;
            }

            public void Dispose()
            {
                _entitiesByIndex = Array.Empty<Entity>();
                _stampsByIndex = Array.Empty<int>();
                _stamp = 0;
            }

            private void EnsureCapacity(int entityIndex)
            {
                if (entityIndex < _stampsByIndex.Length)
                {
                    return;
                }

                int capacity = _stampsByIndex.Length == 0 ? 4 : _stampsByIndex.Length;
                while (capacity <= entityIndex)
                {
                    capacity = checked(capacity * 2);
                }

                Array.Resize(ref _entitiesByIndex, capacity);
                Array.Resize(ref _stampsByIndex, capacity);
            }
        }

        private sealed class EntityPresenceFrameHistory : IDisposable
        {
            private EntityPresenceFrame?[] _frames;
            private int _mask;
            private int _head;
            private int _count;
            private readonly int _maxCapacity;

            public EntityPresenceFrameHistory(int initialCapacity, int maxCapacity)
            {
                _frames = new EntityPresenceFrame?[initialCapacity];
                _mask = initialCapacity - 1;
                _maxCapacity = maxCapacity;
                _frames[0] = new EntityPresenceFrame();
                _frames[0]!.Begin(int.MinValue);
                _count = 1;
            }

            public EntityPresenceFrame BeginWrite(int tick)
            {
                if (tick < Last.Tick)
                {
                    Rollback(tick);
                }

                if (Last.Tick == tick)
                {
                    Last.Begin(tick);
                    return Last;
                }

                EnsureCapacity();
                int index = (_head + _count) & _mask;
                EntityPresenceFrame frame = _frames[index] ?? (_frames[index] = new EntityPresenceFrame());
                frame.Begin(tick);
                _count++;
                return frame;
            }

            public EntityPresenceFrame Get(int tick)
            {
                if (_count == 0 || tick < At(0).Tick)
                {
                    throw new IndexOutOfRangeException($"Entity presence history does not contain tick {tick}");
                }

                int low = 0;
                int high = _count - 1;
                while (low <= high)
                {
                    int middle = (low + high) >> 1;
                    EntityPresenceFrame candidate = At(middle);
                    if (candidate.Tick == tick)
                    {
                        return candidate;
                    }

                    if (candidate.Tick < tick)
                    {
                        low = middle + 1;
                    }
                    else
                    {
                        high = middle - 1;
                    }
                }

                return At(high);
            }

            public void Rollback(int tick)
            {
                while (_count > 0 && Last.Tick > tick)
                {
                    _count--;
                }
            }

            public void ResetBaseline()
            {
                _head = 0;
                _count = 1;
                EntityPresenceFrame frame = _frames[0] ?? (_frames[0] = new EntityPresenceFrame());
                frame.Begin(int.MinValue);
            }

            public void Dispose()
            {
                for (int i = 0; i < _frames.Length; i++)
                {
                    _frames[i]?.Dispose();
                    _frames[i] = null;
                }

                _head = 0;
                _count = 0;
            }

            private EntityPresenceFrame Last => At(_count - 1);

            private EntityPresenceFrame At(int logicalIndex) => _frames[(_head + logicalIndex) & _mask]!;

            private void EnsureCapacity()
            {
                if (_count < _frames.Length)
                {
                    return;
                }

                if (_frames.Length >= _maxCapacity)
                {
                    _head = (_head + 1) & _mask;
                    _count--;
                    return;
                }

                int capacity = Math.Min(_frames.Length << 1, _maxCapacity);
                var expanded = new EntityPresenceFrame?[capacity];
                for (int i = 0; i < _count; i++)
                {
                    expanded[i] = At(i);
                }

                _frames = expanded;
                _head = 0;
                _mask = capacity - 1;
            }
        }

        private abstract class ComponentHistory
        {
            public abstract void PrepareCaptureFrame(int tick);
            public abstract void CapturePreparedFrame();
            public abstract void ResetBaseline();
            public abstract void GoTo(int tick);
            public abstract void Rollback(int tick);
            public abstract void PackSnapshot(WorldData snapshot, int tick, EntityPresenceFrame entityPresence, IReadOnlyDictionary<Entity, int> syncIds);
            public abstract void ApplySnapshot(WorldData snapshot, IReadOnlyDictionary<int, Entity> entitiesBySyncId);
            public abstract void RemoveFrom(Entity entity);
            public abstract void DisposeEntity(Entity entity);
            public abstract void Clear();
            public abstract void Dispose();
        }

        internal struct ComponentRestoreState<T> where T : struct
        {
            public readonly ComponentFrame<T> Frame;
            public readonly Entity[] RestoredEntitiesByIndex;
            public readonly int[] FrameValueIndicesByEntityIndex;
            public readonly int[] RestoreStampsByEntityIndex;
            public readonly int[] SeenStampsByEntityIndex;
            public readonly int RestoreStamp;
            public readonly List<Entity> EntitiesToRemove;

            public ComponentRestoreState(
                ComponentFrame<T> frame,
                Entity[] restoredEntitiesByIndex,
                int[] frameValueIndicesByEntityIndex,
                int[] restoreStampsByEntityIndex,
                int[] seenStampsByEntityIndex,
                int restoreStamp,
                List<Entity> entitiesToRemove)
            {
                Frame = frame;
                RestoredEntitiesByIndex = restoredEntitiesByIndex;
                FrameValueIndicesByEntityIndex = frameValueIndicesByEntityIndex;
                RestoreStampsByEntityIndex = restoreStampsByEntityIndex;
                SeenStampsByEntityIndex = seenStampsByEntityIndex;
                RestoreStamp = restoreStamp;
                EntitiesToRemove = entitiesToRemove;
            }
        }

        internal struct RestoreComponent<T> : IForEachContextEntity<ComponentRestoreState<T>> where T : struct
        {
            public void Invoke(ref ComponentRestoreState<T> state, EntityRef entity, ref T component)
            {
                Entity handle = entity;
                int index = handle.Index;
                if ((uint)index < (uint)state.RestoreStampsByEntityIndex.Length
                    && state.RestoreStampsByEntityIndex[index] == state.RestoreStamp
                    && state.RestoredEntitiesByIndex[index] == handle)
                {
                    component = state.Frame.Values[state.FrameValueIndicesByEntityIndex[index]];
                    state.SeenStampsByEntityIndex[index] = state.RestoreStamp;
                }
                else
                {
                    state.EntitiesToRemove.Add(handle);
                }
            }
        }

        internal delegate void CaptureAction<T>(Entity entity, in T component) where T : struct;

        internal struct CaptureState<T> where T : struct
        {
            private readonly CaptureAction<T> _capture;

            public CaptureState(CaptureAction<T> capture)
            {
                _capture = capture;
            }

            public void Capture(EntityRef entity, in T component)
            {
                Entity handle = entity;
                _capture(handle, in component);
            }
        }

        private sealed class ComponentHistory<T> : ComponentHistory where T : struct
        {
            private readonly World _world;
            private readonly ComponentId _componentId;
            private readonly EntityPresenceHistory _entityPresence;
            private readonly bool _capturesPresence;
            private readonly IOperation<ComponentRestoreState<T>> _restoreOperation;
            private readonly IOperation<CaptureState<T>> _captureOperation;
            private CaptureState<T> _captureState;
            private readonly ComponentFrameHistory<T> _frames;
            private ComponentFrame<T> _captureFrame = null!;
            private Entity[] _restoredEntityByIndex = Array.Empty<Entity>();
            private int[] _frameValueIndexByEntityIndex = Array.Empty<int>();
            private int[] _restoreStampByEntityIndex = Array.Empty<int>();
            private int[] _seenRestoreStampByEntityIndex = Array.Empty<int>();
            private int _restoreStamp;
            private readonly List<Entity> _entitiesToRemove = new();

            public ComponentHistory(
                World world,
                EntityPresenceHistory entityPresence,
                ComponentId componentId,
                int initialCapacity,
                int maxCapacity)
            {
                _world = world;
                _componentId = componentId;
                _entityPresence = entityPresence;
                _capturesPresence = typeof(T) == typeof(SyncId);

                var saveQuery = CreateComponentQuery(world, componentId);
                _frames = new ComponentFrameHistory<T>(initialCapacity, maxCapacity);
                var initialRestoreState = default(ComponentRestoreState<T>);
                _restoreOperation = _world.ForEachEntity(
                    in saveQuery,
                    ref initialRestoreState,
                    _componentId,
                    typeof(RestoreComponent<>));
                _captureState = new CaptureState<T>(Capture);
                _captureOperation = _world.ForEachEntity(
                    in saveQuery,
                    ref _captureState,
                    _componentId,
                    typeof(CaptureComponent<>));
            }

            public Dictionary<Entity, int> GetSyncIdsAt(int tick) => _frames.GetSyncIdsAt(tick);

            public override void PrepareCaptureFrame(int tick)
                => _captureFrame = _frames.BeginWrite(tick);

            public override void CapturePreparedFrame() => _captureOperation.Invoke(ref _captureState);

            public override void ResetBaseline() => _frames.ResetBaseline();

            public void Capture(Entity entity, in T component)
            {
                _captureFrame.Add(entity, in component);
                if (_capturesPresence)
                {
                    _entityPresence.Capture(entity);
                }
            }

            public override void GoTo(int tick)
            {
                ComponentFrame<T> frame = _frames.Get(tick);
                MarkRestoredEntities(frame);

                _entitiesToRemove.Clear();
                var restoreState = new ComponentRestoreState<T>(
                    frame,
                    _restoredEntityByIndex,
                    _frameValueIndexByEntityIndex,
                    _restoreStampByEntityIndex,
                    _seenRestoreStampByEntityIndex,
                    _restoreStamp,
                    _entitiesToRemove);
                _restoreOperation.Invoke(ref restoreState);

                for (int i = 0; i < _entitiesToRemove.Count; i++)
                {
                    _world.Remove(_entitiesToRemove[i], _componentId);
                }

                for (int i = 0; i < frame.Count; i++)
                {
                    Entity entity = frame.Entities[i];
                    if (_seenRestoreStampByEntityIndex[entity.Index] == _restoreStamp)
                    {
                        continue;
                    }

                    if (!_world.IsAlive(entity))
                    {
                        continue;
                    }

                    T value = frame.Values[i];
                    _world.Add(entity, in value);
                }
            }

            public override void Rollback(int tick) => _frames.Rollback(tick);

            public override void PackSnapshot(WorldData snapshot, int tick, EntityPresenceFrame entityPresence, IReadOnlyDictionary<Entity, int> syncIds)
            {
                if (!_frames.TryGet(tick, out ComponentFrame<T> frame))
                {
                    return;
                }

                var output = snapshot.Get<T>();
                for (int i = 0; i < frame.Count; i++)
                {
                    Entity entity = frame.Entities[i];
                    if (!entityPresence.Contains(entity) || !syncIds.TryGetValue(entity, out int syncId))
                    {
                        continue;
                    }

                    output[syncId] = frame.Values[i];
                }
            }

            public override void ApplySnapshot(WorldData snapshot, IReadOnlyDictionary<int, Entity> entitiesBySyncId)
            {
                var components = snapshot.Get<T>();
                foreach (var pair in components)
                {
                    if (!entitiesBySyncId.TryGetValue(pair.Key, out Entity entity))
                    {
                        continue;
                    }

                    T value = pair.Value;
                    if (_world.TryGet<T>(entity, _componentId, out _))
                    {
                        _world.GetRef<T>(entity, _componentId) = value;
                    }
                    else
                    {
                        _world.Add(entity, in value);
                    }
                }
            }

            public override void RemoveFrom(Entity entity)
            {
                if (_world.Has(entity, _componentId))
                {
                    _world.Remove(entity, _componentId);
                }
            }

            public override void DisposeEntity(Entity entity) { }

            public override void Clear()
            {
                _entitiesToRemove.Clear();
                _frames.ResetBaseline();
                _captureFrame = null!;
            }

            public override void Dispose()
            {
                Clear();
                _frames.Dispose();
            }

            private void MarkRestoredEntities(ComponentFrame<T> frame)
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

                for (int i = 0; i < frame.Count; i++)
                {
                    Entity entity = frame.Entities[i];
                    EnsureRestoreCapacity(entity.Index);
                    _restoredEntityByIndex[entity.Index] = entity;
                    _frameValueIndexByEntityIndex[entity.Index] = i;
                    _restoreStampByEntityIndex[entity.Index] = _restoreStamp;
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
                    capacity = checked(capacity * 2);
                }

                Array.Resize(ref _restoredEntityByIndex, capacity);
                Array.Resize(ref _frameValueIndexByEntityIndex, capacity);
                Array.Resize(ref _restoreStampByEntityIndex, capacity);
                Array.Resize(ref _seenRestoreStampByEntityIndex, capacity);
            }

            private static Query CreateComponentQuery(World world, ComponentId componentId)
                => world.WhereAll(componentId);

        }

        internal struct CaptureComponent<T> : IForEachContextEntity<CaptureState<T>> where T : struct
        {
            public void Invoke(ref CaptureState<T> state, EntityRef entity, in T component)
                => state.Capture(entity, in component);
        }

        private sealed class ComponentFrameHistory<T> : IDisposable where T : struct
        {
            private ComponentFrame<T>?[] _frames;
            private int _mask;
            private int _head;
            private int _count;
            private readonly int _maxCapacity;

            public ComponentFrameHistory(int initialCapacity, int maxCapacity)
            {
                _frames = new ComponentFrame<T>?[initialCapacity];
                _mask = initialCapacity - 1;
                _maxCapacity = maxCapacity;
                _frames[0] = new ComponentFrame<T>(int.MinValue);
                _count = 1;
            }

            public ComponentFrame<T> BeginWrite(int tick)
            {
                if (tick < Last.Tick)
                {
                    Rollback(tick);
                }

                if (Last.Tick == tick)
                {
                    Last.Clear();
                    return Last;
                }

                EnsureCapacity();
                int index = (_head + _count) & _mask;
                ComponentFrame<T> frame = _frames[index] ?? (_frames[index] = new ComponentFrame<T>(tick));
                frame.SetTick(tick);
                _count++;
                return frame;
            }

            public ComponentFrame<T> Get(int tick)
            {
                if (!TryGet(tick, out ComponentFrame<T> frame))
                {
                    throw new IndexOutOfRangeException($"History does not contain tick {tick}");
                }

                return frame;
            }

            public bool TryGet(int tick, out ComponentFrame<T> frame)
            {
                if (_count == 0 || tick < At(0).Tick)
                {
                    frame = null!;
                    return false;
                }

                int low = 0;
                int high = _count - 1;
                while (low <= high)
                {
                    int mid = (low + high) >> 1;
                    ComponentFrame<T> candidate = At(mid);
                    if (candidate.Tick == tick)
                    {
                        frame = candidate;
                        return true;
                    }

                    if (candidate.Tick < tick)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid - 1;
                    }
                }

                frame = At(high);
                return true;
            }

            public Dictionary<Entity, int> GetSyncIdsAt(int tick)
            {
                ComponentFrame<T> frame = Get(tick);
                var result = new Dictionary<Entity, int>(frame.Count);
                for (int i = 0; i < frame.Count; i++)
                {
                    if (frame.Values[i] is SyncId syncId)
                    {
                        result[frame.Entities[i]] = syncId.Value;
                    }
                }
                return result;
            }

            public void Rollback(int tick)
            {
                while (_count > 0 && Last.Tick > tick)
                {
                    int index = (_head + _count - 1) & _mask;
                    _frames[index]!.Clear();
                    _count--;
                }
            }

            public void ResetBaseline()
            {
                for (int i = 0; i < _count; i++)
                {
                    At(i).Clear();
                }

                _head = 0;
                _count = 1;
                ComponentFrame<T> baseline = _frames[0] ?? (_frames[0] = new ComponentFrame<T>(int.MinValue));
                baseline.SetTick(int.MinValue);
            }

            public void Dispose()
            {
                for (int i = 0; i < _frames.Length; i++)
                {
                    _frames[i]?.Dispose();
                    _frames[i] = null;
                }

                _count = 0;
            }

            private ComponentFrame<T> Last => At(_count - 1);

            private ComponentFrame<T> At(int logicalIndex) => _frames[(_head + logicalIndex) & _mask]!;

            private void EnsureCapacity()
            {
                if (_count < _frames.Length)
                {
                    return;
                }

                if (_frames.Length >= _maxCapacity)
                {
                    _frames[_head]!.Clear();
                    _head = (_head + 1) & _mask;
                    _count--;
                    return;
                }

                int capacity = Math.Min(_frames.Length << 1, _maxCapacity);
                var expanded = new ComponentFrame<T>?[capacity];
                for (int i = 0; i < _count; i++)
                {
                    expanded[i] = At(i);
                }

                _frames = expanded;
                _head = 0;
                _mask = capacity - 1;
            }
        }

        internal sealed class ComponentFrame<T> : IDisposable where T : struct
        {
            private Entity[] _entities = Array.Empty<Entity>();
            private T[] _values = Array.Empty<T>();

            public int Tick { get; private set; }
            public int Count { get; private set; }
            public Entity[] Entities => _entities;
            public T[] Values => _values;

            public ComponentFrame(int tick)
            {
                Tick = tick;
            }

            public void Add(Entity entity, in T value)
            {
                int count = Count;
                EnsureCapacity(count + 1);
                _entities[count] = entity;
                _values[count] = value;
                Count = count + 1;
            }

            public void SetTick(int tick)
            {
                Clear();
                Tick = tick;
            }

            public void Clear()
            {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() && Count > 0)
                {
                    Array.Clear(_values, 0, Count);
                }

                Count = 0;
            }

            public void Dispose()
            {
                Clear();
                if (_entities.Length != 0)
                {
                    ArrayPool<Entity>.Shared.Return(_entities);
                }

                if (_values.Length != 0)
                {
                    ArrayPool<T>.Shared.Return(_values, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                }

                _entities = Array.Empty<Entity>();
                _values = Array.Empty<T>();
            }

            private void EnsureCapacity(int required)
            {
                if (_entities.Length >= required)
                {
                    return;
                }

                int capacity = Math.Max(4, _entities.Length * 2);
                while (capacity < required)
                {
                    capacity *= 2;
                }

                Entity[] entities = ArrayPool<Entity>.Shared.Rent(capacity);
                T[] values = ArrayPool<T>.Shared.Rent(capacity);
                Array.Copy(_entities, entities, Count);
                Array.Copy(_values, values, Count);
                if (_entities.Length != 0)
                {
                    ArrayPool<Entity>.Shared.Return(_entities);
                }

                if (_values.Length != 0)
                {
                    ArrayPool<T>.Shared.Return(_values, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                }

                _entities = entities;
                _values = values;
            }
        }
    }
}
