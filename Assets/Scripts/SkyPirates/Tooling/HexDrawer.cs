using Assets.Scripts.SkyPirates.Client.Factories;
using DVG;
using DVG.Core;
using DVG.SkyPirates.Client.Factories;
using DVG.SkyPirates.Client.Factories.VMFactories;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.Views.Gameplay;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.Tools.Json;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HexDrawer : MonoBehaviour
{
    [SerializeField]
    private string _loadPath = "Configs/Maps/Map";
    [SerializeField]
    private string _currentTile;
    [SerializeField]
    [Range(0, 30)]
    private int _currentHeight;
    [SerializeField]
    private GameObject _ghostTile;
    [SerializeField]
    [TextArea(10, 50)]
    private string _hexMapJson;

    private HexMap _hexMap;
    private Dictionary<int3, GameObject> _gameObjects;
    private float3? _prevWorldXZDrag;
    private float? _prevYDrag;
    private readonly TileVMFactory _tilemapFactory = new();
    private readonly PathViewFactory<TileView> _tileViewFactory = new();

    private void Awake()
    {
        _hexMap = new HexMap();

        //_hexMap.Data = new Dictionary<int3, TileId>();
        _gameObjects = new Dictionary<int3, GameObject>();

        Load();
    }

    [Button]
    private void Load()
    {
        _hexMap = new ResourcesFactory<HexMap>().Create(_loadPath);
        foreach (var item in _gameObjects)
        {
            DestroyImmediate(item.Value);
        }

        foreach (var item in _hexMap.Data)
        {
            var vm = _tilemapFactory.Create((_hexMap, item.Key));
            var view = _tileViewFactory.Create(IdPathFormatter.FormatVisualPath(vm.TileId));
            view.ViewModel = vm;
            var go = view.gameObject;
            go.transform.SetParent(transform, true);
            _gameObjects[item.Key] = go;
        }
    }

    [Button]
    private void Save()
    {
        _hexMapJson = SerializationUTF8.Serialize(_hexMap);
    }

    private void Update()
    {
        MoveGhost();
        SetDynamicShadowDistance();
        Drag();
        Zoom();
        if (!Input.GetMouseButton(0) &&
            !Input.GetMouseButton(1))
        {
            return;
        }


        var axial = GetAxial();

        Remove(axial);

        if (Input.GetMouseButton(1))
            return;

        Set(axial, new(_currentTile));
    }

    private void MoveGhost()
    {
        var axial = GetAxial();
        var pos = Hex.AxialToWorld(axial);
        if (_ghostTile != null)
            _ghostTile.transform.position = (float3)pos;
    }

    private void Drag()
    {
        if (!Input.GetMouseButton(2) || Input.GetKey(KeyCode.LeftControl))
        {
            _prevWorldXZDrag = null;
            return;
        }

        var current = GetWorldXZ();
        if (_prevWorldXZDrag.HasValue)
        {
            var delta = _prevWorldXZDrag.Value - current;
            float3 camPos = Camera.main.transform.position;
            Camera.main.transform.position = camPos + delta;
        }
        _prevWorldXZDrag = GetWorldXZ();
    }

    private void Zoom()
    {
        if (!Input.GetMouseButton(2) || !Input.GetKey(KeyCode.LeftControl))
        {
            _prevYDrag = null;
            return;
        }

        var current = Input.mousePosition.y / Screen.height;
        if (_prevYDrag.HasValue)
        {
            var delta = new float3(_prevYDrag.Value - current);
            float3 camPos = Camera.main.transform.position;
            delta *= camPos.y;
            delta.x = 0;
            delta.z = -delta.y;
            Camera.main.transform.position = camPos + delta;
        }
        _prevYDrag = current;
    }

    private int3 GetAxial()
    {
        var world = (fix2)GetWorldXZ().xz;
        var axial = Hex.WorldToAxial(world);
        return new int3(axial.x, _currentHeight, axial.y);
    }

    private float3 GetWorldXZ()
    {
        var pos = Input.mousePosition;
        var ray = Camera.main.ScreenPointToRay(pos);
        new Plane(Vector3.down, _currentHeight).Raycast(ray, out var enter);
        return ray.origin + ray.direction * enter;
    }

    private void Remove(int3 axial)
    {
        //_hexMap.Data.Remove(axial);
        if (_gameObjects.Remove(axial, out var go))
            DestroyImmediate(go);
    }

    private void Set(int3 axial, TileId tileId)
    {
        //_hexMap.Data[axial] = tileId;

        var vm = _tilemapFactory.Create((_hexMap, axial));
        var view = _tileViewFactory.Create(IdPathFormatter.FormatVisualPath(vm.TileId));
        view.ViewModel = vm;
        var go = view.gameObject;
        go.transform.SetParent(transform, true);
        _gameObjects[axial] = go;
    }

    private void SetDynamicShadowDistance()
    {
        float minHeight = 0;
        foreach (var item in _hexMap.Data)
            if (item.Key.y < minHeight)
                minHeight = item.Key.y;
        minHeight -= 5;
        var ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height));
        new Plane(Vector3.down, minHeight).Raycast(ray, out var enter);
        QualitySettings.shadowDistance = enter;
        UniversalRenderPipelineAsset urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        urp.shadowDistance = enter;
    }
}
