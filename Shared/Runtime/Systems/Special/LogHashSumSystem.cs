using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.Components;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Text;

namespace DVG.SkyPirates.Shared.Systems.Special
{
    [Obsolete]
    public sealed class LogHashSumSystem : IDeltaTickableExecutor
    {
        private readonly StringBuilder _stringBuilder = new StringBuilder();
        private readonly ComponentQueryCache<QuerySet> _queryCache = new();
        private readonly World _world;
        private readonly WorldComponentIds _componentIds;

        public LogHashSumSystem(World world)
        {
            _world = world;
            _componentIds = WorldComponentIds.For(world);
        }

        public void Tick(int tick, fix deltaTime)
        {
            _ = GetHashSum();
        }

        public (int sum, string info) GetHashSum()
        {
            _stringBuilder.Clear();
            int hash = 0;
            var historyComponents = _componentIds.History;
            for (int i = 0; i < historyComponents.Length; i++)
            {
                var component = historyComponents[i];
                var queries = _queryCache.Get(component);
                var filter = queries.Filter ??= _world.WhereAll(component);
                int componentHash = 0;
                _world.ForEach(in filter, ref componentHash, component, typeof(HashComponent<>)).Invoke(ref componentHash);
                _stringBuilder.AppendLine($"Hash of {_world.Components.GetComponentType(component).Name}: {componentHash}");
                hash += componentHash;
            }

            return (hash, _stringBuilder.ToString());
        }

        internal struct HashComponent<T> : IForEachContext<int> where T : struct
        {
            public void Invoke(ref int hash, in T component) => hash += component.GetHashCode();
        }

        internal sealed class QuerySet
        {
            public QuerySet() { }

            public Query? Filter;
        }
    }
}
