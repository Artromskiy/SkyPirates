using Delta.ECS.Unity;
using SlyPirates.Client;
using UnityEngine;

namespace SlyPirates.Client.Views.Components
{
    public sealed class HealthbarSpawnerComponentView : MonoBehaviour, IEntityView
    {
        private const string HealthbarPrefabPath = "Prefabs/UI/HealthbarView";

        private GameObject? _healthbar;
        private IEntityView[] _views = System.Array.Empty<IEntityView>();

        public async void OnBind(EntityContext context)
        {
            if (_healthbar == null)
            {
                GameObject healthbar = await AddressableLoader.InstantiateStaged(HealthbarPrefabPath, this);
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
            if (_healthbar != null)
            {
                _healthbar.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            AddressableLoader.ReleaseInstance(_healthbar);
        }
    }
}
