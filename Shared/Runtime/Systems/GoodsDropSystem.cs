using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.Components;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;
using System.Diagnostics;

namespace DVG.SkyPirates.Shared.Systems
{
    public class GoodsDropSystem : IDeltaTickableExecutor
    {
        private static readonly fix DropRange = fix.One * 5 / 2;
        private Query? _createDescCache;
        private Query _createDesc => _createDescCache ??= _world.
            WhereAll<Health, GoodsDrop, Position, SyncIdReserve, RandomSeed>().Alive().NotDisabled();

        private readonly World _world;
        private readonly IConfigedEntityFactory<GoodsId> _configedEntityFactory;

        private readonly List<DropInfo> _dropInfos = new();

        public GoodsDropSystem(World world, IConfigedEntityFactory<GoodsId> configedEntityFactory)
        {
            _world = world;
            _configedEntityFactory = configedEntityFactory;
        }

        public void Tick(int tick, fix deltaTime)
        {
            _dropInfos.Clear();

            var dropInfos = _dropInfos;
            var desc = _createDesc;
            _world.ForEach<List<DropInfo>, Health, GoodsDrop, Position, SyncIdReserve, RandomSeed>(in desc, ref dropInfos,
                static (ref List<DropInfo> dropInfos, in Health health, in GoodsDrop goods, in Position position, ref SyncIdReserve syncIdReserve, ref RandomSeed seed) =>
                {
                    if (health > fix.Zero)
                    {
                        return;
                    }

                    int remainingIds = syncIdReserve.RemainingCount();
                    int typesCount = goods.Values.Count;
                    if (typesCount == 0)
                    {
                        return;
                    }

                    Delta.Diagnostics.Debug.Assert(remainingIds >= typesCount);
                    if (remainingIds < typesCount)
                    {
                        return;
                    }

                    int dropsCount = remainingIds;
                    int baseSlots = dropsCount / typesCount;
                    int remainderSlots = dropsCount % typesCount;
                    int i = 0;
                    foreach ((var goodsId, int totalAmount) in goods.Values)
                    {
                        int slots = baseSlots + (i < remainderSlots ? 1 : 0);
                        int baseAmount = totalAmount / slots;
                        int remainderAmount = totalAmount % slots;

                        for (int j = 0; j < slots; j++)
                        {
                            int amount = baseAmount + (j < remainderAmount ? 1 : 0);
                            if (amount == 0)
                            {
                                continue;
                            }

                            var drop = new DropInfo
                            {
                                Position = position,
                                GoodsId = goodsId,
                                Amount = amount,
                                SyncId = syncIdReserve.GetNext(),
                                Direction =
                                {
                                    x = seed.NextFixRange(-DropRange, DropRange),
                                    y = seed.NextFixRange(-DropRange, DropRange),
                                },
                                Rotation = seed.NextFixRange(0, 360)
                            };
                            dropInfos.Add(drop);
                        }
                        i++;
                    }
                }).Invoke(ref dropInfos);
            foreach (var item in _dropInfos)
            {
                EntityParameters parameters = new()
                {
                    SyncId = item.SyncId,
                    RandomSeed = default,
                    SyncIdReserve = default,
                };

                var drop = _configedEntityFactory.Create((item.GoodsId, parameters));
                _world.GetRef<Position>(drop) = item.Position;
                _world.GetRef<Rotation>(drop) = item.Rotation;
                _world.GetOrAdd<GoodsAmount>(drop) = item.Amount;

                _world.GetOrAdd<FlyDestination>(drop) = new()
                {
                    StartPosition = item.Position,
                    EndPosition = item.Position + item.Direction.x_y,
                };
            }
        }

        internal struct DropInfo
        {
            public Position Position;
            public Rotation Rotation;
            public GoodsId GoodsId;
            public SyncId SyncId;
            public fix2 Direction;
            public int Amount;
        }
    }
}
