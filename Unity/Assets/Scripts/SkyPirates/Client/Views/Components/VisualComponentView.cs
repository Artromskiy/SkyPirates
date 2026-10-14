using Delta;
using Delta.ECS.Unity;
using SkyPirates.Shared.Core.Ids;
using SkyPirates.Client.IFactories;
using System;
using UnityEngine;

namespace SkyPirates.Client.Views.Components
{
    public abstract class VisualComponentView<T> : MonoBehaviour, IEntityView
        where T : struct, IId, IEquatable<T>
    {
        private EntityContext _context;
        private GameObject _visual;

        [SerializeField]
        private Transform _visualRoot;
        [SerializeField]
        private bool _randomizeScale;
        [SerializeField]
        private T _id;

        protected EntityContext Context => _context;

        public virtual void OnBind(EntityContext context)
        {
            _context = context;
            UpdateVisual();
        }

        private void Update()
        {
            if (!EntityViewLifetime.TryEnterUpdate(this, _context))
                return;

            if (!_id.Equals(Id))
                UpdateVisual();
        }

        public void UpdateVisual()
        {
            if (_visual != null)
                DestroyImmediate(_visual);

            if (_context.IsAlive)
                _id = Id;
            if (_id.IsNone)
                return;

            string path = IdPathFormatter.FormatVisualPath(_id);
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
                return;

            _visual = Instantiate(prefab, _visualRoot);
            _visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            float scale = _randomizeScale ? UnityEngine.Random.Range(0.9f, 1.1f) : 1;
            _visual.transform.localScale = new float3(scale);
        }

        private T Id => _context.Get<T>();

        private void Awake() => EditorChanges.SubscribeOnChanged(this, SetId);

        protected virtual void OnDestroy()
            => EditorChanges.UnsubscribeOnChanged(this, SetId);

        private void SetId()
        {
            if (_context.IsAlive)
                _context.GetRef<T>() = _id;
            UpdateVisual();
        }
    }
}
