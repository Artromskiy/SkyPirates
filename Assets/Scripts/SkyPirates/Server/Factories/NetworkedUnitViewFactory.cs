using DVG.SkyPirates.Server.IFactories;
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.IViews;
using Unity.Netcode;

namespace DVG.SkyPirates.Server.Factories
{
    public class NetworkedUnitViewFactory : IUnitViewFactory
    {
        public IUnitView Create((UnitId unitId, int level, int merge) parameters)
        {
            var network = NetworkManager.Singleton;
            foreach (var l in network.NetworkConfig.Prefabs.NetworkPrefabsLists)
                foreach (var item in l.PrefabList)
                    if (item.Prefab.name == parameters.unitId.Value && item.Prefab.TryGetComponent<IUnitView>(out _))
                        return network.SpawnManager.InstantiateAndSpawn(item.Prefab.GetComponent<NetworkObject>()).GetComponent<IUnitView>();
            return null;
        }
    }
}
