using Delta;
using Delta.ECS.Unity;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Shared.Core;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Ids;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public sealed class HexTileComponentView : MonoBehaviour, IEntityView
    {
        private GameObject _visual;
        private TileId _tileId;

        public void OnBind(EntityContext context)
        {
            HexTile tile = context.Get<HexTile>();
            transform.position = (float3)Hex.AxialToWorld(new int3(
                tile.Coordinates.x,
                tile.Elevation,
                tile.Coordinates.y));

            TileId tileId = context.Get<TileId>();
            if (_visual != null && _tileId.Equals(tileId))
                return;

            if (_visual != null)
                Destroy(_visual);

            _tileId = tileId;
            if (tileId.IsNone)
                return;

            GameObject prefab = Resources.Load<GameObject>(IdPathFormatter.FormatVisualPath(tileId));
            if (prefab == null)
                return;

            _visual = Instantiate(prefab, transform);
            _visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private void OnDestroy()
        {
            if (_visual != null)
                Destroy(_visual);
        }
    }
}
