using Delta;
using Delta.ECS.Unity;
using SlyPirates.Shared.Core.Ids;
using SlyPirates.Client.IFactories;
using SlyPirates.Client;
using System;
using System.Threading;
using Delta.Diagnostics;
using SlyPirates.Shared.Services.Netcode;
using UnityEngine;
using Debug = Delta.Diagnostics.Debug;

namespace SlyPirates.Client.Views.Components
{
    public interface IVisualInstanceLoaded
    {
        void OnVisualInstanceLoaded();
    }

    public abstract class VisualComponentView<T> : MonoBehaviour, IEntityView
        where T : struct, IId, IEquatable<T>
    {
        private EntityContext _context;
        private GameObject _visual;
        private CancellationTokenSource _visualLoadCancellation;
        private MonoBehaviour[] _components = Array.Empty<MonoBehaviour>();

        [SerializeField]
        private Transform _visualRoot;
        [SerializeField]
        private bool _randomizeScale;
        [SerializeField]
        private T _id;

        protected EntityContext Context => _context;

        private void Awake() => _components = GetComponents<MonoBehaviour>();

        public virtual void OnBind(EntityContext context)
        {
            _context = context;
            if (_visual == null || !_id.Equals(Id))
            {
                UpdateVisual();
            }
            else
            {
                OnVisualLoaded(_visual);
                NotifyVisualInstanceLoaded();
            }
        }

        protected virtual void OnVisualLoaded(GameObject visual) { }

        private void Update()
        {
            if (!_id.Equals(Id))
            {
                UpdateVisual();
            }
        }

        public async void UpdateVisual()
        {
            ReleaseVisual();

            if (_context.IsAlive)
            {
                _id = Id;
            }

            if (_id.IsNone)
            {
                return;
            }

            string path = IdPathFormatter.FormatVisualPath(_id);
            CancellationTokenSource loadCancellation = _visualLoadCancellation = new();
            Trace.Assert(_visualRoot != null, context:this);
            GameObject visual = await AddressableLoader.Instantiate(path, _visualRoot, this, loadCancellation.Token);
            if (_visualLoadCancellation != loadCancellation)
            {
                AddressableLoader.ReleaseInstance(visual);
                return;
            }

            _visual = visual;
            _visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            float scale = _randomizeScale ? UnityEngine.Random.Range(0.9f, 1.1f) : 1;
            _visual.transform.localScale = new float3(scale);
            OnVisualLoaded(visual);
            NotifyVisualInstanceLoaded();
        }

        private void NotifyVisualInstanceLoaded()
        {
            foreach (MonoBehaviour component in _components)
            {
                if (component is IVisualInstanceLoaded listener)
                {
                    listener.OnVisualInstanceLoaded();
                }
            }
        }

        private T Id => _context.Get<T>();

        protected virtual void OnDestroy() => ReleaseVisual();

        private void ReleaseVisual()
        {
            _visualLoadCancellation?.Cancel();
            _visualLoadCancellation?.Dispose();
            _visualLoadCancellation = null;
            AddressableLoader.ReleaseInstance(_visual);
            _visual = null;
        }
    }
}
