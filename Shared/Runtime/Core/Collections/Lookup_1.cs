using System;
using System.Runtime.CompilerServices;

namespace SlyPirates.Shared.Core.Collections
{
	public sealed class Lookup
	{
		private bool[] _items;
		private int _offset;

		public int Length => _items.Length;
		public int Offset => _offset;

		public Lookup(int initialCapacity = 16)
		{
			if (initialCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            _items = new bool[initialCapacity];
			_offset = initialCapacity >> 1;
		}

		public bool this[int id]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Has(id);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				if (value)
                {
                    Add(id);
                }
                else
                {
                    Remove(id);
                }
            }
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Has(int id)
		{
			return TryToIndex(id, out int index) && _items[index];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(int id)
		{
			EnsureCapacity(id);
			_items[id + _offset] = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Remove(int id)
		{
			if (!TryToIndex(id, out int index) || !_items[index])
            {
                return false;
            }

            _items[index] = false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear()
		{
			Array.Clear(_items, 0, _items.Length);
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool TryToIndex(int id, out int index)
		{
			index = id + _offset;
			return (uint)index < (uint)_items.Length;
		}

		private void EnsureCapacity(int id)
		{
			int index = id + _offset;
			if ((uint)index < (uint)_items.Length)
            {
                return;
            }

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
			var newItems = new bool[newSize];

			Array.Copy(
				_items,
				0,
				newItems,
				newOffset - _offset,
				_items.Length
			);

			_items = newItems;
			_offset = newOffset;
		}
	}
}
