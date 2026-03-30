using Assets.Scripts.SkyPirates.Client.Factories;
using DVG.SkyPirates.Client.Factories.VMFactories;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.Views.Gameplay;
using DVG.SkyPirates.Shared.Components.Config;
using System.Collections.Generic;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public class HexMapComponentView : ComponentView
    {
        private readonly Dictionary<int3, GameObject> _gameObjects = new();

        private readonly TileVMFactory _tilemapFactory = new();
        private readonly PathViewFactory<TileView> _tileViewFactory = new();

        public override void OnInject()
        {
            Spawn();
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
                var vm = _tilemapFactory.Create((HexMap, item.Key));
                var view = _tileViewFactory.Create(IdPathFormatter.FormatVisualPath(vm.TileId));
                view.ViewModel = vm;
                var go = view.gameObject;
                go.transform.SetParent(transform, true);
                _gameObjects[item.Key] = go;
            }
        }

        private HexMap HexMap => ViewModel.Get<HexMap>();
    }
}
