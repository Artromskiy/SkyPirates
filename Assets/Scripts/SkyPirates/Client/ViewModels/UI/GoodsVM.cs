using Delta.ECS;
using DVG.Components;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.Systems;
using System;
using System.Collections.Generic;

namespace DVG.SkyPirates.Client.ViewModels.UI
{
    public class GoodsVM : IGoodsVM
    {
        private readonly World _world;
        private readonly IPlayer _player;
        private readonly IEntityRegistry _entityRegistry;

        private readonly Dictionary<GoodsId, int> _goods = new();
        private Query? _squadMembersCache;
        private Query _squadMembers => _squadMembersCache ??= _world.WhereAll<SquadMember, GoodsDrop>().WhereAll<Alive>();

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

                var query = _squadMembers;
                (int SquadId, Dictionary<GoodsId, int> Goods) goodsState = (_player.SquadEntityId.Value, _goods);
                _world.ForEach<(int SquadId, Dictionary<GoodsId, int> Goods), SquadMember, GoodsDrop>(in query, ref goodsState, static (ref (int SquadId, Dictionary<GoodsId, int> Goods) context, ref SquadMember member, ref GoodsDrop drop) =>
                {
                    if (drop.Values == null || member.SquadId != context.SquadId)
                        return;

                    foreach (var item in drop.Values)
                    {
                        if (item.Value <= 0)
                            continue;

                        if (!context.Goods.TryAdd(item.Key, item.Value))
                            context.Goods[item.Key] += item.Value;
                    }
                }).Invoke(ref goodsState);
                return _goods;
            }
        }
    }
}
