using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.IServices.TickableExecutors;
using System.Linq;

namespace SlyPirates.Shared.Systems
{
    public class FramedComponentsSystem : IDeltaTickableExecutor
    {
        private readonly ComponentId[] _framedComponentIds;
        private readonly IOperation[] _clearOperations;

        public FramedComponentsSystem(World world)
        {
            _framedComponentIds = world.GetGroup(Group.Temporary).ToArray();
            _clearOperations = new IOperation[_framedComponentIds.Length];
            for (int i = 0; i < _framedComponentIds.Length; i++)
            {
                var query = world.WhereAll(_framedComponentIds[i]);
                _clearOperations[i] = world.ForEach(in query, _framedComponentIds[i], typeof(ClearFramedComponent<>));
            }
        }

        public void Tick(int tick, fix deltaTime)
        {
            for (int i = 0; i < _framedComponentIds.Length; i++)
            {
                _clearOperations[i].Invoke();
            }

        }

        internal struct ClearFramedComponent<T> : IForEach where T : struct
        {
            public void Invoke(ref T component) => component = default;
        }

    }
}
