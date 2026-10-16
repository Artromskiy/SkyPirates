using SlyPirates.Shared.Core.Collections;
using SlyPirates.Shared.IServices;
using System;
using System.Collections.Generic;

namespace SlyPirates.Shared.Services
{
    public class PooledItemsProvider : IPooledItemsProvider, IDisposable
    {
        private const int MaxCount = 1000;
        private readonly GenericCollection _genericPool = new();
        public void Dispose() => _genericPool.Dispose();

        public T Get<T>() where T : new()
        {
            if (_genericPool.TryGet<Queue<T>>(out var pool) && pool.TryDequeue(out var pooled))
            {
                return pooled;
            }

            return new();
        }

        public void Return<T>(T value) where T : new()
        {
            if (!_genericPool.TryGet<Queue<T>>(out var pool))
            {
                _genericPool.Add(pool = new());
            }

            if (pool.Count < MaxCount)
            {
                pool.Enqueue(value);
            }
        }
    }
}
