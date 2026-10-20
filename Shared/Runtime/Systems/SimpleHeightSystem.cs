using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Core;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
{
    public class SimpleHeightSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Position, CachePosition, Radius, Collide>().Alive().NotDisabled();

        private readonly World _world;
        private readonly IResultOperation<Entity> _findMap;
        private HexMap _mapState;
        private readonly IOperation<HexMap> _adjustHeights;

        public SimpleHeightSystem(World world)
        {
            _world = world;
            Query mapQuery = world.WhereAll<HexMap>();
            _findMap = mapQuery.First();

            var desc = _desc;
            _adjustHeights = world.ForEach<HexMap, Position>(in desc, ref _mapState,
                static (ref HexMap map, ref Position position) =>
                {
                    var axial = Hex.WorldToAxial(position);
                    bool zero = map.Data.ContainsKey(axial);
                    var up = new int3(0, 1, 0);
                    bool p1 = map.Data.ContainsKey(axial + up);
                    bool p2 = map.Data.ContainsKey(axial + up * 2);
                    bool p3 = map.Data.ContainsKey(axial + up * 3);
                    bool m1 = map.Data.ContainsKey(axial - up);

                    if (zero && !p1 && !p2)
                    {
                        position.Value.y = Hex.AxialToWorldY(axial.y);
                        return;
                    }
                    else if (p1 && !p2 && !p3)
                    {
                        position.Value.y = Hex.AxialToWorldY(axial.y + 1);
                        return;
                    }
                    else if (m1 && !zero && !p1)
                    {
                        position.Value.y = Hex.AxialToWorldY(axial.y - 1);
                        return;
                    }

                    Delta.Diagnostics.Debug.Assert(false, context: "Wrong height behaviour detected");
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            var hexMapEntity = _findMap.Invoke();
            if (!hexMapEntity.IsValid)
            {
                return;
            }

            var hexMap = _world.Get<HexMap>(hexMapEntity);
            if (hexMap.Data == null)
            {
                return;
            }

            _mapState = hexMap;
            _adjustHeights.Invoke(ref _mapState);
        }
    }
}
