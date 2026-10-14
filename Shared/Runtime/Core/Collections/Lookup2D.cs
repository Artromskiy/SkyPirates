using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DVG.SkyPirates.Shared.Core.Collections
{
	public sealed class Lookup2D<T>
	{
		private T[] _items;
		private bool[] _has;

		private int _width;
		private int _height;

		private int _offsetX;
		private int _offsetY;

		public int Width => _width;
		public int Height => _height;

		public int OffsetX => _offsetX;
		public int OffsetY => _offsetY;

		public Lookup2D(int initialSize = 8)
		{
			if (initialSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(initialSize));

			_width = initialSize;
			_height = initialSize;

			_items = new T[_width * _height];
			_has = new bool[_items.Length];

			_offsetX = _width >> 1;
			_offsetY = _height >> 1;
		}

		public T this[int x, int y]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Get(x, y);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => Set(x, y, value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool ContainsKey(int x, int y) => Has(x, y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetValue(int x, int y, out T value)
		{
			if (TryToIndex(x, y, out int index) && _has[index])
			{
				value = _items[index];
				return true;
			}

			value = default!;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Remove(int x, int y)
		{
			if (!TryToIndex(x, y, out int index) || !_has[index])
				return false;

			_has[index] = false;
			_items[index] = default!;
			return true;
		}

		public void Clear()
		{
			Array.Clear(_has, 0, _has.Length);
			if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
				Array.Clear(_items, 0, _items.Length);
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private T Get(int x, int y)
		{
			int index = ToIndex(x, y);
			if (!_has[index])
				throw new KeyNotFoundException($"Key ({x},{y}) not found");

			return _items[index];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Set(int x, int y, T value)
		{
			EnsureCapacity(x, y);
			int index = ToIndex(x, y);
			_items[index] = value;
			_has[index] = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool Has(int x, int y) =>
			TryToIndex(x, y, out int index) && _has[index];


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int ToIndex(int x, int y)
		{
			int ix = x + _offsetX;
			int iy = y + _offsetY;
			return iy * _width + ix;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool TryToIndex(int x, int y, out int index)
		{
			int ix = x + _offsetX;
			int iy = y + _offsetY;

			if ((uint)ix >= (uint)_width || (uint)iy >= (uint)_height)
			{
				index = 0;
				return false;
			}

			index = iy * _width + ix;
			return true;
		}

		private void EnsureCapacity(int x, int y)
		{
			int ix = x + _offsetX;
			int iy = y + _offsetY;

			if ((uint)ix < (uint)_width && (uint)iy < (uint)_height)
				return;

			int newWidth = _width;
			int newHeight = _height;
			int newOffsetX = _offsetX;
			int newOffsetY = _offsetY;

			int minX = Math.Min(ix, 0);
			int maxX = Math.Max(ix, newWidth - 1);
			int minY = Math.Min(iy, 0);
			int maxY = Math.Max(iy, newHeight - 1);

			while (minX < 0 || maxX >= newWidth)
			{
				int grow = newWidth;
				newWidth <<= 1;
				newOffsetX += grow >> 1;
				minX += grow >> 1;
				maxX += grow >> 1;
			}

			while (minY < 0 || maxY >= newHeight)
			{
				int grow = newHeight;
				newHeight <<= 1;
				newOffsetY += grow >> 1;
				minY += grow >> 1;
				maxY += grow >> 1;
			}

			Resize(newWidth, newHeight, newOffsetX, newOffsetY);
		}

		private void Resize(int newWidth, int newHeight, int newOffsetX, int newOffsetY)
		{
			var newItems = new T[newWidth * newHeight];
			var newHas = new bool[newItems.Length];

			int dx = newOffsetX - _offsetX;
			int dy = newOffsetY - _offsetY;

			for (int y = 0; y < _height; y++)
			{
				int srcRow = y * _width;
				int dstRow = (y + dy) * newWidth + dx;

				Array.Copy(_items, srcRow, newItems, dstRow, _width);
				Array.Copy(_has, srcRow, newHas, dstRow, _width);
			}

			_items = newItems;
			_has = newHas;
			_width = newWidth;
			_height = newHeight;
			_offsetX = newOffsetX;
			_offsetY = newOffsetY;
		}
	}
}
