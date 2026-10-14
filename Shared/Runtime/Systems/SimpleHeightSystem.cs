using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;

namespace DVG.SkyPirates.Shared.Systems
{
    public class SimpleHeightSystem : ITransientDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Position, CachePosition, Radius, Collide>().Alive().NotDisabled();

        private readonly World _world;

        public SimpleHeightSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            var hexMapEntity = _world.WhereAll<HexMap>()
                .First()
                .Invoke();
            if (!hexMapEntity.IsValid)
            {
                return;
            }

            var hexMap = _world.Get<HexMap>(hexMapEntity);
            if (hexMap.Data == null)
            {
                return;
            }

            var desc = _desc;
            _world.ForEach<HexMap, Position>(in desc, ref hexMap,
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
                }).Invoke(ref hexMap);
        }
    }
}
