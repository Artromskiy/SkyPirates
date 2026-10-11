using Delta;
using DVG.Ids;
using DVG.SkyPirates.Client.IFactories;
using System;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public abstract class VisualComponentView<T> : ComponentView
        where T : struct, IId, IEquatable<T>
    {
        private GameObject _visual;
        [SerializeField]
        private Transform _visualRoot;
        [SerializeField]
        private bool _randomizeScale;

        [SerializeField]
        private T _id;

        public override void OnBound() => UpdateVisual();

        public void UpdateVisual()
        {
            if (_visual != null)
                DestroyImmediate(_visual);

            _id = Id;
            if (_id.IsNone)
                return;

            string path = IdPathFormatter.FormatVisualPath(Id);
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
                return;

            _visual = Instantiate(prefab, _visualRoot);
            _visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            float scale = _randomizeScale ? UnityEngine.Random.Range(0.9f, 1.1f) : 1;
            _visual.transform.localScale = new float3(scale);
        }

        private T Id
        {
            get => Get<T>();
            set => Set<T>() = value;
        }

        public override void Tick()
        {
            if (_id.Equals(Id))
                return;
            UpdateVisual();
        }

        private void Awake() => OnEditorChanged += SetId;
        protected override void OnDestroy()
        {
            OnEditorChanged -= SetId;
            base.OnDestroy();
        }

        private void SetId()
        {
            if (IsAlive)
                Set<T>() = _id;
            UpdateVisual();
        }
    }
}
