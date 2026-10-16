using System;
using System.Collections.Generic;
using Delta.ECS;
using DVG.SkyPirates.Shared.Components;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Data;

namespace DVG.SkyPirates.Shared.Ecs
{
    // Owns history resources, never gameplay services. Presence is counted across all retained
    // frames, including the future retained by GoTo. PendingDestroy itself is not a presence.
    public sealed class WorldHistory : IWorldHistory
    {
        private readonly World _world;
        private readonly HistoryColumn[] _columns;
        private readonly HistoryColumn<SyncId> _syncIds;
        private readonly ComponentId[] _componentIds;
        private readonly FrameRing<PresenceFrame> _frames;
        private readonly EntityValueMap<int> _references = new();
        private readonly EntityValueMap<int> _owned = new();
        private readonly EntityValueMap<bool> _present = new();
        private readonly List<Entity> _disappeared = new();
        private readonly List<Entity> _release = new();
        private readonly List<Entity> _transitions = new();
        private readonly Stack<PresenceFrame> _recycled = new();
        private readonly IOperation _collectOwned;
        private readonly IOperation _collectPresent;
        private readonly IOperation _collectPending;
        private readonly IOperation _collectAbsent;
        private readonly IOperation _collectRestored;
        private readonly HistoryResource[] _resources;
        private readonly int _capacity;
        private Entity[] _batch = Array.Empty<Entity>();
        private PresenceFrame _capture = null!;
        private bool _disposed;

        public WorldHistory(World world, ReadOnlySpan<ComponentId> componentIds, int initialCapacity, int maxCapacity)
        {
            if (initialCapacity <= 0 || maxCapacity < initialCapacity)
                throw new ArgumentOutOfRangeException(nameof(maxCapacity));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _capacity = maxCapacity;
            _frames = new FrameRing<PresenceFrame>(initialCapacity);
            ComponentId pendingId = world.Components.GetPrimary<PendingDestroy>();
            var columns = new List<HistoryColumn>();
            var ids = new List<ComponentId>();
            var visitor = new CreateColumns(world, this, columns);
            for (int i = 0; i < componentIds.Length; i++)
                if (componentIds[i] != pendingId && !ids.Contains(componentIds[i]))
                {
                    ids.Add(componentIds[i]);
                    world.Components.Visit(componentIds[i], visitor);
                }
            ComponentId syncId = world.Components.GetPrimary<SyncId>();
            if (!ids.Contains(syncId))
            {
                ids.Add(syncId);
                world.Components.Visit(syncId, visitor);
            }
            _columns = columns.ToArray();
            _componentIds = ids.ToArray();
            _syncIds = (HistoryColumn<SyncId>)columns.Find(c => c.Id == syncId)!;
            Query query = world.WhereAll<SyncId>();
            _collectOwned = world.ForEachEntity(in query, (EntityRef entity, in SyncId id) =>
                _owned.Set(entity, id.Value));
            Query present = query.WhereNone<PendingDestroy>();
            _collectPresent = world.ForEachEntity(in present, (EntityRef entity, in SyncId id) =>
            {
                Entity handle = entity;
                _owned.Set(handle, id.Value);
                _capture.Entities.Add(handle);
                _present.Set(handle, true);
            });
            Query pending = query.WhereAll<PendingDestroy>();
            _collectPending = world.ForEachEntity(in pending, (EntityRef entity, in SyncId id) =>
                _owned.Set(entity, id.Value));
            Query active = world.WhereNone<PendingDestroy>();
            _collectAbsent = world.ForEachEntity(in active, (EntityRef entity) =>
            {
                Entity handle = entity;
                if (Owns(handle) && !IsPresent(handle)) _transitions.Add(handle);
            });
            Query hidden = world.WhereAll<PendingDestroy>();
            _collectRestored = world.ForEachEntity(in hidden, (EntityRef entity) =>
            {
                Entity handle = entity;
                if (IsPresent(handle)) _transitions.Add(handle);
            });
            var resources = new List<HistoryResource>();
            var disposeVisitor = new CreateResources(world, this, resources);
            foreach (ComponentId id in world.GetGroup(Group.All))
                world.Components.Visit(id, disposeVisitor);
            _resources = resources.ToArray();
        }

        public int OldestTick => _frames.Count == 0 ? throw new InvalidOperationException("History is empty.") : _frames.First.Tick;
        public int NewestTick => _frames.Count == 0 ? throw new InvalidOperationException("History is empty.") : _frames.Last.Tick;

        public void Save(int tick)
        {
            ThrowIfDisposed();
            if (_frames.Count != 0 && tick < NewestTick)
                throw new InvalidOperationException("Rollback before replacing the future of history.");
            bool replace = _frames.Count != 0 && tick == NewestTick;
            PresenceFrame? previous = _frames.Count == 0 ? null : _frames.Last;
            _capture = _recycled.Count != 0 ? _recycled.Pop() : new PresenceFrame(previous?.Entities.Count ?? _world.AliveEntityCount);
            _capture.Tick = tick;
            _present.Clear();
            _collectPresent.Invoke();
            _collectPending.Invoke();
            CaptureResources();
            _disappeared.Clear();
            if (previous != null)
                foreach (Entity entity in previous.Entities)
                    if (!_present.TryGetValue(entity, out _)) _disappeared.Add(entity);

            for (int i = 0; i < _columns.Length; i++)
                _columns[i].Capture(tick, replace, _disappeared);
            if (replace) Recycle(_frames.RemoveLast());
            _frames.Add(_capture);
            foreach (Entity entity in _capture.Entities)
            {
                _references.TryGetValue(entity, out int count);
                _references.Set(entity, count + 1);
            }
            while (_frames.Count > _capacity) DropFirst();
            _capture = null!;
            ReleaseUnreferenced();
        }

        public void GoTo(int tick)
        {
            ThrowIfDisposed();
            PresenceFrame frame = _frames.Find(tick);
            _present.Clear();
            foreach (Entity entity in frame.Entities) _present.Set(entity, true);
            // Remember unsaved transient entities too; their resources have the same owner.
            _collectOwned.Invoke();
            CaptureResources();
            _transitions.Clear();
            _collectAbsent.Invoke();
            _world.Add<PendingDestroy>(Batch(_transitions));
            _transitions.Clear();
            _collectRestored.Invoke();
            _world.Remove<PendingDestroy>(Batch(_transitions));
            for (int i = 0; i < _columns.Length; i++) _columns[i].Restore(tick);
            ReleaseUnreferenced();
        }

        public void Rollback(int tick)
        {
            GoTo(tick);
            while (_frames.Last.Tick > tick) Recycle(_frames.RemoveLast());
            for (int i = 0; i < _columns.Length; i++) _columns[i].Rollback(tick);
            ReleaseUnreferenced();
        }

        public WorldData GetSnapshot(int tick)
        {
            ThrowIfDisposed();
            PresenceFrame frame = _frames.Find(tick);
            var ids = _syncIds.StateAt(tick);
            var syncIds = new Dictionary<Entity, int>(frame.Entities.Count);
            foreach (Entity entity in frame.Entities)
                if (ids.TryGetValue(entity, out SyncId id)) syncIds.Add(entity, id.Value);
            var snapshot = new WorldData();
            for (int i = 0; i < _columns.Length; i++) _columns[i].Export(snapshot, tick, syncIds);
            return snapshot;
        }

        public void ApplySnapshot(WorldData snapshot, int tick)
        {
            ThrowIfDisposed();
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var sourceIds = snapshot.Get<SyncId>();
            foreach (var pair in sourceIds)
                if (pair.Key != pair.Value.Value)
                    throw new ArgumentException("Snapshot SyncId keys must match their component values.", nameof(snapshot));
            var retained = new Dictionary<int, Entity>();
            for (int i = 0; i < _owned.Count; i++) retained[_owned.GetValueAt(i)] = _owned.GetEntityAt(i);
            Query query = _world.WhereAll<SyncId>();
            _world.ForEachEntity(in query, (EntityRef entity, in SyncId id) =>
            {
                retained[id.Value] = entity;
                _owned.Set(entity, id.Value);
            }).Invoke();
            CaptureResources();
            ResetFrames();
            var entities = new Dictionary<int, Entity>(sourceIds.Count);
            var ids = new List<ComponentId>(_columns.Length);
            foreach (var pair in sourceIds)
            {
                ids.Clear();
                for (int i = 0; i < _columns.Length; i++)
                    if (_columns[i].ContainsSnapshot(snapshot, pair.Key)) ids.Add(_columns[i].Id);
                ComponentId[] components = ids.ToArray();
                Entity entity;
                if (retained.TryGetValue(pair.Key, out entity) && _world.IsAlive(entity))
                {
                    // Reset all historical membership in two combined structural transitions.
                    _world.Remove(entity, _componentIds);
                    _world.Remove<PendingDestroy>(entity);
                    _world.Add(entity, components);
                }
                else entity = _world.Create(components);
                entities.Add(pair.Key, entity);
                _owned.Set(entity, pair.Key);
            }
            for (int i = 0; i < _columns.Length; i++) _columns[i].Import(snapshot, entities);
            _release.Clear();
            for (int i = 0; i < _owned.Count; i++)
            {
                Entity entity = _owned.GetEntityAt(i);
                if (!entities.TryGetValue(_owned.GetValueAt(i), out Entity keep) || keep != entity) _release.Add(entity);
            }
            ReleaseQueued();
            for (int i = 0; i < _resources.Length; i++) _resources[i].Prune();
            Save(tick);
        }

        public void Clear()
        {
            ThrowIfDisposed();
            _collectOwned.Invoke();
            CaptureResources();
            ResetFrames();
            ReleaseUnreferenced();
            for (int i = 0; i < _resources.Length; i++) _resources[i].Prune();
        }

        private void ResetFrames()
        {
            while (_frames.Count > 0) Recycle(_frames.RemoveLast());
            _references.Clear();
            _present.Clear();
            for (int i = 0; i < _columns.Length; i++) _columns[i].Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _collectOwned.Invoke();
            ResetFrames();
            _release.Clear();
            for (int i = 0; i < _owned.Count; i++) _release.Add(_owned.GetEntityAt(i));
            ReleaseQueued();
            _disposed = true;
        }

        private void DropFirst()
        {
            Recycle(_frames.RemoveFirst());
            for (int i = 0; i < _columns.Length; i++) _columns[i].DropFirst();
        }

        private void Recycle(PresenceFrame frame)
        {
            foreach (Entity entity in frame.Entities)
                if (_references.TryGetValue(entity, out int count))
                {
                    if (count == 1) _references.Remove(entity);
                    else _references.Set(entity, count - 1);
                }
            frame.Entities.Clear();
            _recycled.Push(frame);
        }

        private ReadOnlySpan<Entity> Batch(List<Entity> entities)
        {
            if (_batch.Length < entities.Count) Array.Resize(ref _batch, Math.Max(entities.Count, _batch.Length * 2));
            entities.CopyTo(_batch);
            return _batch.AsSpan(0, entities.Count);
        }

        private void ReleaseUnreferenced()
        {
            _release.Clear();
            for (int i = 0; i < _owned.Count; i++)
            {
                Entity entity = _owned.GetEntityAt(i);
                if (!_references.TryGetValue(entity, out _) &&
                    (!_world.IsAlive(entity) || _world.Has<PendingDestroy>(entity))) _release.Add(entity);
            }
            ReleaseQueued();
        }

        private void ReleaseQueued()
        {
            if (_release.Count == 0) return;
            CaptureResources();
            if (_batch.Length < _release.Count) Array.Resize(ref _batch, _release.Count);
            int count = 0;
            foreach (Entity entity in _release)
            {
                _owned.Remove(entity);
                if (_world.IsAlive(entity)) _batch[count++] = entity;
            }
            ReadOnlySpan<Entity> batch = _batch.AsSpan(0, count);
            for (int i = 0; i < _resources.Length; i++) _resources[i].Release(_release, batch);
            _world.Destroy(batch);
            _release.Clear();
        }

        private void CaptureResources()
        {
            for (int i = 0; i < _resources.Length; i++) _resources[i].Capture();
        }

        internal bool Owns(Entity entity) => _owned.TryGetValue(entity, out _);
        internal bool IsPresent(Entity entity) => _present.TryGetValue(entity, out _);
        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorldHistory));
        }

        private readonly struct CreateColumns : IStructComponentVisitor
        {
            private readonly World _world;
            private readonly WorldHistory _owner;
            private readonly List<HistoryColumn> _columns;
            public CreateColumns(World world, WorldHistory owner, List<HistoryColumn> columns)
            { _world = world; _owner = owner; _columns = columns; }
            public void Visit<T>(ComponentId id) where T : struct => _columns.Add(new HistoryColumn<T>(_world, _owner, id));
        }

        private readonly struct CreateResources : IStructComponentVisitor<IDisposable>
        {
            private readonly World _world;
            private readonly WorldHistory _owner;
            private readonly List<HistoryResource> _resources;
            public CreateResources(World world, WorldHistory owner, List<HistoryResource> resources)
            { _world = world; _owner = owner; _resources = resources; }
            public void Visit<T>(ComponentId id) where T : struct, IDisposable
                => _resources.Add(new HistoryResource<T>(_world, _owner, id, static (ref T component) => component.Dispose()));
        }

        private sealed class PresenceFrame : IHistoryFrame
        {
            public readonly List<Entity> Entities;

            public PresenceFrame(int capacity) => Entities = new List<Entity>(capacity);

            public int Tick { get; set; }
        }
    }

    internal abstract class HistoryResource
    {
        public abstract void Capture();
        public abstract void Release(IReadOnlyList<Entity> entities, ReadOnlySpan<Entity> batch);
        public abstract void Prune();
    }

    internal delegate void HistoryDisposeAction<T>(ref T component) where T : struct;

    // Resources are owned independently of the currently restored component membership.
    // Removing a component during GoTo must not lose the resource held by a retained frame.
    internal sealed class HistoryResource<T> : HistoryResource where T : struct
    {
        private readonly World _world;
        private readonly WorldHistory _owner;
        private readonly ComponentId _id;
        private readonly HistoryDisposeAction<T> _dispose;
        private readonly EntityValueMap<List<T>> _values = new();
        private readonly IOperation _capture;
        private readonly Query _query;

        public HistoryResource(World world, WorldHistory owner, ComponentId id, HistoryDisposeAction<T> dispose)
        {
            _world = world; _owner = owner; _id = id; _dispose = dispose;
            var context = this;
            _query = world.WhereAll(id);
            _capture = world.ForEachEntity(in _query, ref context, id, typeof(HistoryCaptureResource<>));
        }

        public override void Capture() => _capture.Invoke();
        internal void Remember(Entity entity, in T value)
        {
            if (!_owner.Owns(entity)) return;
            if (!_values.TryGetValue(entity, out List<T> values)) _values.Set(entity, values = new List<T>());
            if (!values.Contains(value)) values.Add(value);
        }
        internal void DisposeCurrent(Entity entity, ref T value)
        {
            if (_values.TryGetValue(entity, out List<T> values)) values.Remove(value);
            _dispose(ref value);
        }
        public override void Release(IReadOnlyList<Entity> entities, ReadOnlySpan<Entity> batch)
        {
            // Dispose currently attached values through typed refs, while the chunk lease is valid.
            var context = this;
            _world.ForEachEntity(batch, in _query, ref context, _id, typeof(HistoryReleaseResource<>)).Invoke();
            foreach (Entity entity in entities)
                if (_values.TryGetValue(entity, out List<T> values))
                {
                    foreach (T item in values)
                    {
                        T value = item;
                        _dispose(ref value);
                    }
                    _values.Remove(entity);
                }
        }
        public override void Prune()
        {
            // Old frames have been cleared. Only the resource attached to the current
            // descriptor may remain; historical values removed by a restore can now go.
            for (int i = _values.Count - 1; i >= 0; i--)
            {
                Entity entity = _values.GetEntityAt(i);
                List<T> values = _values.GetValueAt(i);
                bool hasValue = _world.IsAlive(entity) && _world.Has(entity, _id);
                T current = hasValue ? _world.Get<T>(entity, _id) : default;
                for (int j = values.Count - 1; j >= 0; j--)
                    if (!hasValue || !EqualityComparer<T>.Default.Equals(current, values[j]))
                    {
                        T value = values[j];
                        _dispose(ref value);
                        values.RemoveAt(j);
                    }
                if (values.Count == 0) _values.Remove(entity);
            }
        }
    }
    internal struct HistoryCaptureResource<T> : IForEachContextEntity<HistoryResource<T>> where T : struct
    {
        public void Invoke(ref HistoryResource<T> context, EntityRef entity, in T component)
            => context.Remember(entity, in component);
    }
    internal struct HistoryReleaseResource<T> : IForEachContextEntity<HistoryResource<T>> where T : struct
    {
        public void Invoke(ref HistoryResource<T> context, EntityRef entity, ref T component)
            => context.DisposeCurrent(entity, ref component);
    }

    internal interface IHistoryFrame { int Tick { get; } }

    // One ring discipline for presence and component change frames. Capacity is a count of
    // saved frames (the initial frame counts too), not a separate gameplay retirement window.
    internal sealed class FrameRing<T> where T : class, IHistoryFrame
    {
        private T?[] _items;
        private int _head;
        public int Count { get; private set; }
        public FrameRing(int capacity = 4) => _items = new T?[capacity];
        public T First => At(0);
        public T Last => At(Count - 1);
        public T At(int index) => _items[(_head + index) % _items.Length]!;
        public void Add(T frame)
        {
            if (Count == _items.Length)
            {
                var items = new T?[_items.Length * 2];
                for (int i = 0; i < Count; i++) items[i] = At(i);
                _items = items;
                _head = 0;
            }
            _items[(_head + Count++) % _items.Length] = frame;
        }
        public T RemoveFirst()
        {
            T value = First;
            _items[_head] = null;
            _head = (_head + 1) % _items.Length;
            Count--;
            return value;
        }
        public T RemoveLast()
        {
            T value = Last;
            _items[(_head + --Count) % _items.Length] = null;
            return value;
        }
        public T Find(int tick)
        {
            if (Count == 0 || tick < First.Tick || tick > Last.Tick)
                throw new ArgumentOutOfRangeException(nameof(tick), $"Tick {tick} is outside the retained history.");
            int low = 0, high = Count - 1;
            while (low <= high)
            {
                int middle = (low + high) >> 1;
                int candidate = At(middle).Tick;
                if (candidate == tick) return At(middle);
                if (candidate < tick) low = middle + 1; else high = middle - 1;
            }
            throw new ArgumentOutOfRangeException(nameof(tick), $"Tick {tick} was not saved.");
        }
    }

    internal abstract class HistoryColumn
    {
        public ComponentId Id { get; }
        protected HistoryColumn(ComponentId id) => Id = id;
        public abstract void Capture(int tick, bool replace, List<Entity> disappeared);
        public abstract void Restore(int tick);
        public abstract void Rollback(int tick);
        public abstract void DropFirst();
        public abstract void Export(WorldData snapshot, int tick, Dictionary<Entity, int> syncIds);
        public abstract bool ContainsSnapshot(WorldData snapshot, int id);
        public abstract void Import(WorldData snapshot, Dictionary<int, Entity> entities);
        public abstract void Clear();
    }

    internal sealed class HistoryColumn<T> : HistoryColumn where T : struct
    {
        private readonly World _world;
        private readonly WorldHistory _owner;
        private readonly FrameRing<ComponentHistoryFrame<T>> _frames = new();
        private readonly Stack<ComponentHistoryFrame<T>> _recycled = new();
        private readonly List<Entity> _add = new();
        private readonly EntityValueMap<T> _baseline = new();
        private readonly EntityValueMap<T> _state = new();
        private readonly EntityValueMap<int> _indices = new();
        private readonly EntityValueMap<bool> _changed = new();
        private readonly ComponentChangeOperation<T> _changeOperation;
        private readonly IOperation _captureOperation;
        private readonly List<Entity> _remove = new();
        private Entity[] _batch = Array.Empty<Entity>();
        private ComponentHistoryFrame<T> _capture = null!;
        private int? _stateTick;
        private int _worldTick;

        public HistoryColumn(World world, WorldHistory owner, ComponentId id) : base(id)
        {
            _world = world;
            _owner = owner;
            Query query = world.WhereAll(id);
            var context = this;
            _captureOperation = world.ForEachEntity(in query, ref context, id, typeof(HistoryCaptureComponent<>));
            _changeOperation = world.ForEachChange<T>(id, OnChanged);
        }

        public override void Capture(int tick, bool replace, List<Entity> disappeared)
        {
            // Keep the state before this frame for reversible change rows. Restoring a
            // nearby tick then touches only the intervening changes in either direction.
            int previous = _frames.Count - (replace ? 2 : 1);
            if (previous >= 0) StateAt(_frames.At(previous).Tick);
            else
            {
                _state.Clear();
                for (int i = 0; i < _baseline.Count; i++) _state.Set(_baseline.GetEntityAt(i), _baseline.GetValueAt(i));
                _stateTick = null;
            }
            _indices.Clear();
            if (replace)
            {
                _capture = _frames.Last;
                for (int i = 0; i < _capture.Rows.Count; i++) _indices.Set(_capture.Rows[i].Entity, i);
            }
            else
            {
                _capture = _recycled.Count != 0 ? _recycled.Pop() : new ComponentHistoryFrame<T>();
                _capture.Tick = tick;
                _frames.Add(_capture);
            }
            if (_stateTick >= tick) _stateTick = null;
            _changeOperation.Invoke();
            if (_frames.Count == 1) _captureOperation.Invoke();
            // A tag removes the entire logical entity once, including unchanged columns.
            foreach (Entity entity in disappeared) Record(entity, false, default);
            _capture = null!;
            _worldTick = tick;
        }

        internal void CaptureRow(Entity entity, in T value)
        {
            if (!_owner.IsPresent(entity)) return;
            Record(entity, true, value);
        }

        private void OnChanged(Entity entity, ChangeKind kind, ref T value)
        {
            if (_capture == null)
            {
                if (_owner.Owns(entity)) _changed.Set(entity, true);
                return;
            }
            if (!_owner.IsPresent(entity)) return;
            if (kind == ChangeKind.Removed) Record(entity, false, default);
            else Record(entity, true, value);
        }

        private void Record(Entity entity, bool hasValue, in T value)
        {
            if (_indices.TryGetValue(entity, out int index))
            {
                _capture.Rows[index] = new HistoryRow<T>(entity, hasValue, value);
            }
            else
            {
                bool hadValue = _state.TryGetValue(entity, out T previous);
                _indices.Set(entity, _capture.Rows.Count);
                _capture.Rows.Add(new HistoryRow<T>(entity, hasValue, value));
                _capture.Previous.Add(new HistoryRow<T>(entity, hadValue, previous));
            }
        }

        public EntityValueMap<T> StateAt(int tick)
        {
            _frames.Find(tick);
            if (_stateTick == tick) return _state;
            if (_stateTick.HasValue && _stateTick.Value > tick && _stateTick.Value >= _frames.First.Tick)
            {
                for (int i = _frames.Count - 1; i >= 0; i--)
                {
                    ComponentHistoryFrame<T> current = _frames.At(i);
                    if (current.Tick <= tick) break;
                    if (current.Tick <= _stateTick.Value) Undo(current, _state);
                }
                _stateTick = tick;
            }
            if (!_stateTick.HasValue || _stateTick.Value < _frames.First.Tick)
            {
                _state.Clear();
                for (int i = 0; i < _baseline.Count; i++) _state.Set(_baseline.GetEntityAt(i), _baseline.GetValueAt(i));
                _stateTick = null;
            }
            for (int i = 0; i < _frames.Count; i++)
            {
                ComponentHistoryFrame<T> current = _frames.At(i);
                if (current.Tick > tick) break;
                if (!_stateTick.HasValue || current.Tick > _stateTick.Value) Apply(current, _state);
            }
            _stateTick = tick;
            return _state;
        }

        public override void Restore(int tick)
        {
            StateAt(tick);
            _changed.Clear();
            _changeOperation.Invoke();
            int first = Math.Min(_worldTick, tick);
            int last = Math.Max(_worldTick, tick);
            for (int i = _frames.Count - 1; i >= 0; i--)
            {
                ComponentHistoryFrame<T> frame = _frames.At(i);
                if (frame.Tick <= first) break;
                if (frame.Tick > last) continue;
                foreach (HistoryRow<T> row in frame.Rows) _changed.Set(row.Entity, true);
            }

            _add.Clear();
            _remove.Clear();
            for (int i = 0; i < _changed.Count; i++)
            {
                Entity entity = _changed.GetEntityAt(i);
                bool exists = _world.Has(entity, Id);
                if (_state.TryGetValue(entity, out T value))
                {
                    if (exists) _world.GetRef<T>(entity, Id) = value;
                    else _add.Add(entity);
                }
                else if (exists) _remove.Add(entity);
            }
            _world.Remove(Batch(_remove), Id);
            _world.Add(Batch(_add), Id);
            foreach (Entity entity in _add)
            {
                _state.TryGetValue(entity, out T value);
                _world.GetRef<T>(entity, Id) = value;
            }
            // Consume writes made by this restore; other observers still receive them.
            _changeOperation.Invoke();
            _changed.Clear();
            _worldTick = tick;
        }

        private ReadOnlySpan<Entity> Batch(List<Entity> entities)
        {
            if (_batch.Length < entities.Count) Array.Resize(ref _batch, Math.Max(entities.Count, _batch.Length * 2));
            entities.CopyTo(_batch);
            return _batch.AsSpan(0, entities.Count);
        }

        private void Recycle(ComponentHistoryFrame<T> frame)
        {
            frame.Rows.Clear();
            frame.Previous.Clear();
            _recycled.Push(frame);
        }

        public override void Rollback(int tick)
        {
            while (_frames.Count > 0 && _frames.Last.Tick > tick) Recycle(_frames.RemoveLast());
            if (_stateTick > tick) _stateTick = null;
        }
        public override void DropFirst()
        {
            ComponentHistoryFrame<T> frame = _frames.RemoveFirst();
            Apply(frame, _baseline);
            Recycle(frame);
        }
        private static void Apply(ComponentHistoryFrame<T> frame, EntityValueMap<T> state)
        {
            foreach (HistoryRow<T> row in frame.Rows)
                if (row.HasValue) state.Set(row.Entity, row.Value); else state.Remove(row.Entity);
        }
        private static void Undo(ComponentHistoryFrame<T> frame, EntityValueMap<T> state)
        {
            for (int i = frame.Rows.Count - 1; i >= 0; i--)
            {
                HistoryRow<T> row = frame.Previous[i];
                if (row.HasValue) state.Set(row.Entity, row.Value); else state.Remove(row.Entity);
            }
        }
        public override void Export(WorldData snapshot, int tick, Dictionary<Entity, int> syncIds)
        {
            EntityValueMap<T> values = StateAt(tick);
            var output = snapshot.Get<T>();
            // Canonical SyncId order keeps the hash independent of archetype/storage order.
            var ordered = new SortedDictionary<int, T>();
            for (int i = 0; i < values.Count; i++)
                if (syncIds.TryGetValue(values.GetEntityAt(i), out int id)) ordered.Add(id, values.GetValueAt(i));
            foreach (var pair in ordered) output.Add(pair.Key, pair.Value);
        }
        public override bool ContainsSnapshot(WorldData snapshot, int id) => snapshot.Get<T>().ContainsKey(id);
        public override void Import(WorldData snapshot, Dictionary<int, Entity> entities)
        {
            foreach (var pair in snapshot.Get<T>())
                if (entities.TryGetValue(pair.Key, out Entity entity)) _world.GetRef<T>(entity, Id) = pair.Value;
        }
        public override void Clear()
        {
            while (_frames.Count > 0) Recycle(_frames.RemoveLast());
            _baseline.Clear(); _state.Clear(); _indices.Clear(); _changed.Clear(); _stateTick = null;
            _worldTick = 0;
        }
    }
    internal struct HistoryCaptureComponent<T> : IForEachContextEntity<HistoryColumn<T>> where T : struct
    {
        public void Invoke(ref HistoryColumn<T> context, EntityRef entity, in T component)
            => context.CaptureRow(entity, in component);
    }
    internal sealed class ComponentHistoryFrame<T> : IHistoryFrame where T : struct
    {
        public readonly List<HistoryRow<T>> Rows = new();
        public readonly List<HistoryRow<T>> Previous = new();

        public int Tick { get; set; }
    }
    internal readonly struct HistoryRow<T> where T : struct
    {
        public readonly Entity Entity;
        public readonly bool HasValue;
        public readonly T Value;
        public HistoryRow(Entity entity, bool hasValue, in T value) { Entity = entity; HasValue = hasValue; Value = value; }
    }
}
