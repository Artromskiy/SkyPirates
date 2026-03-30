using Arch.Core;
using DVG.Components;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.Systems;
using System.Collections.Generic;

namespace DVG.SkyPirates.Client.ViewModels.UI
{
    public class GoodsVM : IGoodsVM
    {
        private readonly World _world;
        private readonly IPlayer _player;
        private readonly IEntityRegistry _entityRegistry;

        private readonly Dictionary<GoodsId, int> _goods = new();

        public GoodsVM(World world, IPlayer player, IEntityRegistry entityRegistry)
        {
            _world = world;
            _player = player;
            _entityRegistry = entityRegistry;
        }

        public IReadOnlyDictionary<GoodsId, int> Goods
        {
            get
            {
                _goods.Clear();
                if (_player.SquadEntityId == null)
                    return _goods;

                if (!_entityRegistry.TryGet(_player.SquadEntityId.Value, out var squad))
                    return _goods;

                if (!_world.IsAlive(squad))
                    return _goods;

                var squadGoods = _world.Get<GoodsDrop>(squad);
                if (squadGoods.Values != null)
                    foreach (var item in squadGoods.Values)
                        _goods.Add(item.Key, item.Value);

                var desc = new QueryDescription().WithAll<SquadMember>().Alive();
                _world.Query(desc, (ref SquadMember member, ref GoodsDrop drop, ref SyncId syncId) =>
                {
                    if (drop.Values == null)
                        return;

                    if (member.SquadId != _player.SquadEntityId)
                        return;

                    foreach (var item in drop.Values)
                    {
                        if (item.Value <= 0)
                            continue;

                        if (!_goods.TryAdd(item.Key, item.Value))
                            _goods[item.Key] += item.Value;
                    }
                });
                return _goods;
            }
        }
    }
}