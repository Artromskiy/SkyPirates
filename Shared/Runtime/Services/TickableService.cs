using SlyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;
using System.Linq;

namespace SlyPirates.Shared.Services
{
    public class TickableService<T> : ITickableService<T>
        where T : ITickable
    {
        private readonly T[] _executors;

        public TickableService(IEnumerable<T> executors)
        {
            _executors = executors.ToArray();
        }

        public void Tick(int tick)
        {
            foreach (var item in _executors)
            {
                item.Tick(tick);
            }
        }
    }
}
