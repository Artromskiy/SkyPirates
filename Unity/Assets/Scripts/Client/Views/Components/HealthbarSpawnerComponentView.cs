using Delta.ECS.Unity;
using System.Threading;
using UnityEngine;

namespace SlyPirates.Client.Views.Components
{
    public sealed class HealthbarSpawnerComponentView : MonoBehaviour, IEntityView
    {
        private const string HealthbarPrefabPath = "Prefabs/UI/HealthbarView";

        private GameObject? _healthbar;
        private IEntityView[] _views = System.Array.Empty<IEntityView>();
        private CancellationTokenSource? _loadCancellation;

        public async void OnBind(EntityContext context)
        {
            if (_healthbar == null)
            {
                CancelLoad();
                CancellationTokenSource loadCancellation = _loadCancellation = new();
                GameObject healthbar = await AddressableLoader.InstantiateStaged(
                    HealthbarPrefabPath,
                    this,
                    loadCancellation.Token);
                if (_loadCancellation != loadCancellation)
                {
                    AddressableLoader.ReleaseInstance(healthbar);
                    return;
                }

                _loadCancellation = null;
                loadCancellation.Dispose();
                if (healthbar == null)
                {
                    return;
                }

                _healthbar = healthbar;
                _views = _healthbar.GetComponentsInChildren<IEntityView>(true);
                HealthbarCanvasView.RegisterSpawnedView(_healthbar.transform);
            }
            foreach (IEntityView view in _views)
            {
                view.OnBind(context);
            }

            _healthbar.SetActive(isActiveAndEnabled);
        }

        private void OnEnable()
        {
            if (_healthbar != null)
            {
                _healthbar.SetActive(true);
            }
        }

        private void OnDisable()
        {
            CancelLoad();

            if (_healthbar != null)
            {
                _healthbar.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            CancelLoad();
            AddressableLoader.ReleaseInstance(_healthbar);
        }

        private void CancelLoad()
        {
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
            _loadCancellation = null;
        }
    }
}
