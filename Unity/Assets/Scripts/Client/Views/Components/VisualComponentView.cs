using Delta;
using Delta.ECS.Unity;
using SlyPirates.Shared.Core.Ids;
using SlyPirates.Client.IFactories;
using SlyPirates.Client;
using System;
using System.Threading;
using UnityEngine;

namespace SlyPirates.Client.Views.Components
{
    public abstract class VisualComponentView<T> : MonoBehaviour, IEntityView
        where T : struct, IId, IEquatable<T>
    {
        private EntityContext _context;
        private GameObject _visual;
        private CancellationTokenSource _visualLoadCancellation;

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
            if (_visual == null || !_id.Equals(Id))
            {
                UpdateVisual();
            }
        }

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
