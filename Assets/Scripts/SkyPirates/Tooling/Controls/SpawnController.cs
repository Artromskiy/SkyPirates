using Arch.Core;
using DVG.Components;
using DVG.Ids;
using DVG.SkyPirates.Client.DI;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.IServices;
using NaughtyAttributes;
using System;
using UnityEngine;

namespace DVG.SkyPirates.Tooling.Controls
{
    [Inject]
    public abstract class SpawnController : MonoBehaviour { }

    [Inject]
    public abstract class SpawnController<T> : SpawnController
        where T : struct, IId, IEquatable<T>
    {
        [SerializeField]
        [Dropdown(nameof(Ids))]
        [OnValueChanged(nameof(OnValidate))]
        private T _id;

        [ReadOnly]
        [SerializeField]
        [ShowAssetPreview(512, 512)]
        private GameObject _preview;

        [SerializeField]
        private bool _randomizeId;
        [SerializeField]
        private bool _randomizeRotation;

        [Inject]
        private readonly IEntityRegistry _entityRegistryService;
        [Inject]
        private readonly IConfigedEntityFactory<T> _configedEntityFactory;

        [Inject]
        private readonly World _world;

        protected abstract T[] Ids { get; }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            Spawn(GetWorldXZ());
        }

        private void Spawn(float3 position)
        {
            var entityId = _entityRegistryService.Reserve();
            var reserve = _entityRegistryService.Reserve(10);
            var rnd = new RandomSeed() { Value = new System.Random().Next() };
            var id = _randomizeId ?
                Ids[UnityEngine.Random.Range(0, Ids.Length)] :
                _id;
            EntityParameters parameters = new(entityId, reserve, rnd);
            var entity = _configedEntityFactory.Create((id, parameters));
            _world.Get<Position>(entity).Value = (fix3)position;
            var rotation = _randomizeRotation ? UnityEngine.Random.Range(0, 360) : 0;
            _world.Get<Rotation>(entity).Value = rotation;
        }

        private float3 GetWorldXZ()
        {
            var pos = Input.mousePosition;
            var ray = Camera.main.ScreenPointToRay(pos);
            new Plane(Vector3.down, 0).Raycast(ray, out var enter);
            return ray.origin + ray.direction * enter;
        }

        private void OnValidate()
        {
            _preview = Resources.Load<GameObject>(IdPathFormatter.FormatVisualPath(_id));
        }
    }
}