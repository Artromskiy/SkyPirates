using Delta.ECS;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Client.IServices;
using SlyPirates.Client.IViewModels;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Ids;
using SlyPirates.Shared.IServices;
using System.Collections.Generic;

namespace SlyPirates.Client.ViewModels.UI
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
                {
                    return _goods;
                }

                if (!_entityRegistry.TryGet(_player.SquadEntityId.Value, out var squad))
                {
                    return _goods;
                }

                if (!_world.IsAlive(squad))
                {
                    return _goods;
                }

                var goods = _world.Get<GoodsDrop>(squad);
                if (goods.Values != null)
                {
                    foreach (var item in goods.Values)
                    {
                        _goods.Add(item.Key, item.Value);
                    }
                }

                return _goods;
            }
        }
    }
}
