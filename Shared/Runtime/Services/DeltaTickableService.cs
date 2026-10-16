using Delta;
using SlyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;
using System.Linq;

namespace SlyPirates.Shared.Services
{
    public class DeltaTickableService<T> : IDeltaTickableService<T>
        where T : IDeltaTickable
    {
        private readonly T[] _executors;

        public DeltaTickableService(IEnumerable<T> executors)
        {
            _executors = executors.ToArray();
        }

        public void Tick(int tick, fix deltaTime)
        {
            for (int i = 0; i < _executors.Length; i++)
            {
                _executors[i].Tick(tick, deltaTime);
            }
        }
    }
}
