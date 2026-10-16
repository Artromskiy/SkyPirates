using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SlyPirates.Shared.Core.Collections
{
	public sealed class Lookup<T>
	{
		private T[] _items;
		private bool[] _has;
		private int _offset;

		public int Length => _items.Length;
		public int Offset => _offset;

		public Lookup(int initialCapacity = 16)
		{
			if (initialCapacity <= 0)
				throw new ArgumentOutOfRangeException(nameof(initialCapacity));

			_items = new T[initialCapacity];
			_has = new bool[initialCapacity];
			_offset = initialCapacity >> 1;
		}

		public T this[int id]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Get(id);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => Set(id, value);
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool ContainsKey(int id) => Has(id);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetValue(int id, out T value)
		{
			if (TryToIndex(id, out int index) && _has[index])
			{
				value = _items[index];
				return true;
			}

			value = default!;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref T GetOrAddRef(int id)
		{
			int index = id + _offset;
			if ((uint)index >= (uint)_items.Length)
			{
				Grow(id);
				index = id + _offset;
			}

			_has[index] = true;
			return ref _items[index];
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Remove(int id)
		{
			if (!TryToIndex(id, out int index) || !_has[index])
				return false;

			_has[index] = false;
			_items[index] = default!;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear()
		{
			Array.Clear(_has, 0, _has.Length);
			if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
				Array.Clear(_items, 0, _items.Length);
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private T Get(int id)
		{
			int index = ToIndex(id);
			if (!_has[index])
				throw new KeyNotFoundException($"Key '{id}' not found");

			return _items[index];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Set(int id, T value) => GetOrAddRef(id) = value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool Has(int id) =>
			TryToIndex(id, out int index) && _has[index];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int ToIndex(int id) => id + _offset;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool TryToIndex(int id, out int index)
		{
			index = id + _offset;
			return (uint)index < (uint)_items.Length;
		}

		private void Grow(int id)
		{
			int index = id + _offset;
			if ((uint)index < (uint)_items.Length)
				return;

			int newSize = _items.Length;
			int newOffset = _offset;

			int minIndex = Math.Min(index, 0);
			int maxIndex = Math.Max(index, newSize - 1);

			while (minIndex < 0 || maxIndex >= newSize)
			{
				int grow = newSize;
				newSize <<= 1;
				newOffset += grow >> 1;
				minIndex += grow >> 1;
				maxIndex += grow >> 1;
			}

			Resize(newSize, newOffset);
		}

		private void Resize(int newSize, int newOffset)
		{
			var newItems = new T[newSize];
			var newHas = new bool[newSize];

			int shift = newOffset - _offset;

			Array.Copy(_items, 0, newItems, shift, _items.Length);
			Array.Copy(_has, 0, newHas, shift, _has.Length);

			_items = newItems;
			_has = newHas;
			_offset = newOffset;
		}
	}
}
