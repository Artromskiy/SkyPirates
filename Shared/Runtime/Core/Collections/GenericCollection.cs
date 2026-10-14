using DVG.SkyPirates.Shared.Core.Collections;
using System;

namespace DVG.SkyPirates.Shared.Core.Collections
{
	public sealed class GenericCollection : IDisposable
	{
		private int _usedEntries;

		private readonly Lookup<Entry> _entries = new();
		private readonly Lookup<int> _typeToEntry = new();

		private int GetOrCreateEntryId<T>()
		{
			var typeId = Entry<T>.TypeId;
			if (!_typeToEntry.TryGetValue(typeId, out var entryId))
			{
				entryId = _usedEntries++;
				_typeToEntry[typeId] = entryId;
			}

			return entryId;
		}

		private bool TryGetEntryId<T>(out int entryId)
		{
			return _typeToEntry.TryGetValue(Entry<T>.TypeId, out entryId);
		}

		public void Add<T>(T value)
		{
			int id = GetOrCreateEntryId<T>();

			if (!_entries.TryGetValue(id, out var entry))
				_entries[id] = entry = new Entry<T>();

			var typed = (Entry<T>)entry;
			typed.Value = value;
			typed.Exists = true;
		}

		public bool TryGet<T>(out T value)
		{
			if (!TryGetEntryId<T>(out var id) ||
				!_entries.TryGetValue(id, out var entry) ||
				!entry.Exists)
			{
				value = default!;
				return false;
			}

			if (entry is not Entry<T> typed)
				throw new InvalidOperationException();

			value = typed.Value;
			return true;
		}

		public bool Has<T>()
		{
			return TryGetEntryId<T>(out var id)
				&& _entries.TryGetValue(id, out var entry)
				&& entry.Exists;
		}

		public void Remove<T>()
		{
			if (TryGetEntryId<T>(out var id) &&
				_entries.TryGetValue(id, out var entry))
				entry.Exists = false;
		}

		public void Clear()
		{
			for (int i = 0; i < _usedEntries; i++)
				if (_entries.TryGetValue(i, out var entry))
					entry.Exists = false;
		}

		public void Dispose()
		{
			_usedEntries = 0;
			_entries.Clear();
			_typeToEntry.Clear();
		}

		private abstract class Entry
		{
			public static int LastTypeId;
			public bool Exists;
		}

		private sealed class Entry<T> : Entry
		{
			public static readonly int TypeId = LastTypeId++;
			public T Value = default!;
		}
	}
}
