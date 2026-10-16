using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace SlyPirates.Shared.Ecs
{
    internal struct History<T> : IDisposable where T : struct
    {
        private T?[] _values;
        private int[] _ticks;

        private int _mask;

        private int _head;
        private int _count;

        private readonly int _maxCapacity;

        [IgnoreDataMember]
        public int Capacity { get; private set; }
        [IgnoreDataMember]
        public readonly int Count => _count;

        public History(int initialCapacity, int maxCapacity)
        {
            if (!((maxCapacity & (maxCapacity - 1)) == 0))
                throw new ArgumentException("History maxCapacity should be power of two");

            if (!((initialCapacity & (initialCapacity - 1)) == 0))
                throw new ArgumentException("History initialCapacity should be power of two");

            if (initialCapacity > maxCapacity)
                throw new ArgumentException("History initialCapacity should be less or equal to maxCapacity");

            _maxCapacity = maxCapacity;
            Capacity = initialCapacity;

            _values = ArrayPool<T?>.Shared.Rent(Capacity);
            _ticks = ArrayPool<int>.Shared.Rent(Capacity);

            _mask = Capacity - 1;
            _head = 0;
            _count = 1;

            _ticks[0] = int.MinValue;
            _values[0] = null;
        }

        public T? this[int tick]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => Get(tick);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Set(tick, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Set(int tick, T? value)
        {
            Delta.Diagnostics.Debug.Assert(RollbackAssertion(tick), context: "Rollback called implicitly");

            if (_count > 0)
            {
                int lastIdx = Index(_count - 1);
                if (_ticks[lastIdx] == tick)
                {
                    _values[lastIdx] = value;
                    return;
                }
                //if (NullableMarshalEquilityComparer.Default.EqualsRef(ref _values[lastIdx], ref value))
                //    return;
            }

            EnsureCapacity();

            int index = TailIndex();

            _ticks[index] = tick;
            _values[index] = value;

            _count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly T? Get(int tick)
        {
            int count = _count;
            if (count == 0)
                throw new IndexOutOfRangeException($"History is empty and does not contain tick {tick}");

            int lastIdx = Index(count - 1);
            if (_ticks[lastIdx] <= tick)
                return _values[lastIdx];

            var head = _head;
            int oldestTick = _ticks[head];

            if (tick < oldestTick)
                throw new IndexOutOfRangeException($"History does not contain tick {tick}. Last found tick {_ticks[lastIdx]}");

            if (tick == oldestTick)
                return _values[head];

            int idx = BinarySearch(tick, count);

            return _values[idx];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rollback(int toTick)
        {
            int count = _count;
            if (count == 0)
                return;

            while (count > 0 && GetTick(count - 1) > toTick)
                count--;

            _count = count;

            if (count == 0)
                _head = 0;
        }

        public readonly T? GetLast(out int tick)
        {
            if (_count == 0)
                throw new InvalidOperationException("History is empty");
            var idx = Index(_count - 1);
            tick = _ticks[idx];
            return _values[idx];
        }

        private bool RollbackAssertion(int toTick)
        {
            if (_count != 0 && toTick < GetTick(_count - 1))
            {
                Rollback(toTick);
                return false;
            }
            return true;
        }

        private readonly int BinarySearch(int tick, int count)
        {
            int lo = 0;
            int hi = count - 1;

            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int idx = Index(mid);
                int midTick = _ticks[idx];

                if (midTick == tick)
                    return idx;

                if (midTick < tick)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            return Index(hi);
        }

        private void EnsureCapacity()
        {
            if (_count < Capacity)
                return;

            if (Capacity >= _maxCapacity)
            {
                _head = Inc(_head);
                _count--;
                return;
            }

            int newCapacity = Capacity << 1;
            if (newCapacity > _maxCapacity)
                newCapacity = _maxCapacity;

            Resize(newCapacity);
        }

        private void Resize(int newCapacity)
        {
            var newValues = ArrayPool<T?>.Shared.Rent(newCapacity);
            var newTicks = ArrayPool<int>.Shared.Rent(newCapacity);

            for (int i = 0; i < _count; i++)
            {
                int src = Index(i);
                newValues[i] = _values[src];
                newTicks[i] = _ticks[src];
            }

            ArrayPool<T?>.Shared.Return(_values, true);
            ArrayPool<int>.Shared.Return(_ticks, true);

            _values = newValues;
            _ticks = newTicks;

            Capacity = newCapacity;
            _head = 0;
            _mask = newCapacity - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int TailIndex() => (_head + _count) & _mask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int Index(int index) => (_head + index) & _mask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int Inc(int value) => (value + 1) & _mask;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int GetTick(int tick) => _ticks[Index(tick)];

        public readonly void Dispose()
        {
            ArrayPool<T?>.Shared.Return(_values, true);
            ArrayPool<int>.Shared.Return(_ticks, true);
        }


        private class NullableMarshalEquilityComparer : IEqualityComparer<T?>
        {
            private static readonly int _size = Marshal.SizeOf<T>();
            public static NullableMarshalEquilityComparer Default { get; } = new();


            public bool EqualsRef(ref T? x, ref T? y)
            {
                ref var xRef = ref Unsafe.As<T?, NullableReadable>(ref x);
                ref var yRef = ref Unsafe.As<T?, NullableReadable>(ref y);

                if (xRef.hasValue != yRef.hasValue)
                    return false;

                if (!xRef.hasValue)
                    return true;

                var xBytes = MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref xRef.value), _size);
                var yBytes = MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref yRef.value), _size);
                return xBytes.SequenceEqual(yBytes);
            }

            public bool Equals(T? x, T? y) => EqualsRef(ref x, ref y);
            public int GetHashCode(T? obj) => throw new NotImplementedException();
        }

        private struct NullableReadable
        {
            public readonly bool hasValue;
            public T value;
        }
    }
}
