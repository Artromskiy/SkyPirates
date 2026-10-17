using Delta;
using Delta.ECS.Unity;
using SlyPirates.Client.IFactories;
using SlyPirates.Shared.Core;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Ids;
using UnityEngine;

namespace SlyPirates.Client.Views.Components
{
    public sealed class HexTileComponentView : MonoBehaviour, IEntityView
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private bool _hasColorOverride;
        [SerializeField] private Color _colorOverride = Color.white;

        private GameObject _visual;
        private TileId _tileId;
        private MaterialPropertyBlock _propertyBlock;

        private void OnEnable()
        {
            if (_hasColorOverride && transform.childCount > 0)
            {
                _visual = transform.GetChild(0).gameObject;
                ApplyColor(_colorOverride);
            }
        }

        public void OnBind(EntityContext context)
        {
            HexTile tile = context.Get<HexTile>();
            transform.position = (float3)Hex.AxialToWorld(new int3(
                tile.Coordinates.x,
                tile.Elevation,
                tile.Coordinates.y));

            TileId tileId = context.Get<TileId>();
            SetVisual(tileId);
            _hasColorOverride = true;
            _colorOverride = tile.Color == default ? Color.white : new Color(
                (float)tile.Color.x,
                (float)tile.Color.y,
                (float)tile.Color.z,
                (float)tile.Color.w);
            ApplyColor(_colorOverride);
        }

        public void SetEditorPreview(int column, int row, int elevation, string tileId, Color color)
        {
            int2 axial = Hex.OffsetToAxial(new int2(column, row));
            transform.position = (float3)Hex.AxialToWorld(new int3(axial.x, elevation, axial.y));
            _hasColorOverride = true;
            _colorOverride = color;
            SetVisual((TileId)tileId);
            ApplyColor(color);
        }

        private void SetVisual(TileId tileId)
        {
            if (_visual != null && _tileId.Equals(tileId))
            {
                return;
            }

            if (_visual != null)
            {
                Destroy(_visual);
            }

            _tileId = tileId;
            if (tileId.IsNone)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(IdPathFormatter.FormatVisualPath(tileId));
            if (prefab == null)
            {
                return;
            }

            _visual = Instantiate(prefab, transform);
            _visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private void ApplyColor(Color color)
        {
            if (_visual == null)
            {
                return;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            foreach (Renderer target in _visual.GetComponentsInChildren<Renderer>(true))
            {
                target.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, color);
                _propertyBlock.SetColor(ColorId, color);
                target.SetPropertyBlock(_propertyBlock);
            }
        }

        private void OnDestroy()
        {
            if (_visual != null)
            {
                Destroy(_visual);
            }
        }
    }
}
