#nullable enable
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.IViews;
using UnityEngine;

namespace DVG.SkyPirates.Client.Factories
{
    internal class UnitViewFactory : IUnitViewFactory
    {
        public IUnitView Create((UnitId unitId, int level, int merge) parameters)
        {
            var prefab = Resources.Load<GameObject>($"Prefabs/Units/{parameters.unitId.value}");
            var go = Object.Instantiate(prefab);
            return go.GetComponent<IUnitView>();
        }
    }
}
