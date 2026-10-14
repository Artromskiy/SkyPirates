using Delta;
using Delta.ECS.Unity;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Shared.Components.Config;
using System.Collections.Generic;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public class HexMapComponentView : MonoBehaviour, IEntityView
    {
        private readonly Dictionary<int3, GameObject> _gameObjects = new();
        private EntityContext _context;

        public void OnBind(EntityContext context)
        {
            _context = context;
            Spawn();
        }

        private void Update()
        {
            EntityViewLifetime.TryEnterUpdate(this, _context);
        }

        private void Spawn()
        {
            foreach (var item in _gameObjects)
                DestroyImmediate(item.Value);
            _gameObjects.Clear();
            if (HexMap.Data == null)
                return;

            foreach (var item in HexMap.Data)
            {
                string path = IdPathFormatter.FormatVisualPath(item.Value);
                GameObject prefab = Resources.Load<GameObject>(path);
                if (prefab == null)
                    continue;

                GameObject tile = Instantiate(prefab, transform);
                tile.transform.position = (float3)Hex.AxialToWorld(item.Key);
                _gameObjects[item.Key] = tile;
            }
        }

        private HexMap HexMap => _context.Get<HexMap>();
    }
}
