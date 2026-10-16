using System;
using System.Collections.Generic;
using Delta.ECS;

namespace DVG.SkyPirates.Shared.Ecs
{
    internal sealed class EntityValueMap<T>
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
            if (page != null && page.DenseIndices[slot] != 0 && page.Entities[slot] == entity)
            {
                value = page.Values[slot];
                return true;
            }

            value = default!;
            return false;
        }

        public void Set(Entity entity, in T value)
            => GetValueRefOrAddDefault(entity, out _) = value;

        public ref T GetValueRefOrAddDefault(Entity entity, out bool exists)
        {
            Page page = GetPage(entity.Index, true)!;
            int slot = entity.Index & PageMask;
            exists = page.DenseIndices[slot] != 0 && page.Entities[slot] == entity;
            if (!exists)
            {
                if (page.DenseIndices[slot] != 0) RemoveAt(page, slot);

                page.Entities[slot] = entity;
                page.DenseIndices[slot] = _entities.Count + 1;
                _entities.Add(entity);
            }

            return ref page.Values[slot];
        }

        public bool Remove(Entity entity)
        {
            Page? page = GetPage(entity.Index, false);
            int slot = entity.Index & PageMask;
            if (page == null || page.DenseIndices[slot] == 0 || page.Entities[slot] != entity)
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
            }

            _entities.Clear();
        }

        private void RemoveAt(Page page, int slot)
        {
            int removeIndex = page.DenseIndices[slot] - 1;
            int lastIndex = _entities.Count - 1;
            if (removeIndex != lastIndex)
            {
                Entity moved = _entities[lastIndex];
                _entities[removeIndex] = moved;
                Page movedPage = GetPage(moved.Index, false)!;
                movedPage.DenseIndices[moved.Index & PageMask] = removeIndex + 1;
            }

            _entities.RemoveAt(lastIndex);
            page.Entities[slot] = default;
            page.Values[slot] = default!;
            page.DenseIndices[slot] = 0;
        }

        private Page? GetPage(int entityIndex, bool create)
        {
            if (entityIndex < 0) throw new ArgumentOutOfRangeException(nameof(entityIndex));

            int pageIndex = entityIndex >> PageShift;
            int rootIndex = pageIndex >> (DirectoryShift * 2);
            int middleIndex = (pageIndex >> DirectoryShift) & DirectoryMask;
            int leafIndex = pageIndex & DirectoryMask;

            if (!create)
                return _root.Length == 0 ? null : _root[rootIndex]?.Children[middleIndex]?.Pages[leafIndex];

            if (_root.Length == 0) _root = new MiddleDirectory?[RootSize];
            MiddleDirectory middle = _root[rootIndex] ??= new MiddleDirectory();
            LeafDirectory leaf = middle.Children[middleIndex] ??= new LeafDirectory();
            return leaf.Pages[leafIndex] ??= new Page();
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
            // Zero means absent; occupied slots store their dense index plus one.
            public readonly int[] DenseIndices = new int[PageSize];
        }
    }
}
