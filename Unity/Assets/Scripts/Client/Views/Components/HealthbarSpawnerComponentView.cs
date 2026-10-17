using Delta.ECS.Unity;
using UnityEngine;

namespace SlyPirates.Client.Views.Components
{
    public sealed class HealthbarSpawnerComponentView : MonoBehaviour, IEntityView
    {
        private const string HealthbarPrefabPath = "Prefabs/UI/HealthbarView";

        private GameObject? _healthbar;
        private IEntityView[] _views = System.Array.Empty<IEntityView>();

        public void OnBind(EntityContext context)
        {
            if (_healthbar == null)
            {
                GameObject prefab = Resources.Load<GameObject>(HealthbarPrefabPath);
                _healthbar = Instantiate(prefab);
                _views = _healthbar.GetComponentsInChildren<IEntityView>(true);
                HealthbarCanvasView.RegisterSpawnedView(_healthbar.transform);
            }
            foreach (IEntityView view in _views)
            {
                view.OnBind(context);
            }

            _healthbar.SetActive(isActiveAndEnabled);
        }

        private void OnEnable() { if (_healthbar != null)
            {
                _healthbar.SetActive(true);
            }
        }
        private void OnDisable() { if (_healthbar != null)
            {
                _healthbar.SetActive(false);
            }
        }
        private void OnDestroy() { if (_healthbar != null)
            {
                Destroy(_healthbar);
            }
        }
    }
}
