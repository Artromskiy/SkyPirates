using SlyPirates.Client.IViewModels;
using SlyPirates.Client.IViews;
using UnityEngine;

namespace SlyPirates.Client.Views
{
    public class HealthbarCanvasView : View<IHealthbarCanvasVM>
    {
        private static HealthbarCanvasView _current;

        private void OnEnable() => _current = this;

        private void OnDisable()
        {
            if (_current == this)
            {
                _current = null;
            }
        }

        public override void OnInject()
        {
            ViewModel.OnViewCreated += Register;
        }

        public static void RegisterSpawnedView(Transform healthbarView)
        {
            if (_current != null && healthbarView != null)
            {
                _current.Register(healthbarView);
            }
        }

        public void Register(Transform healthbarView)
        {
            if (healthbarView != null)
            {
                healthbarView.SetParent(transform, false);
            }
        }
    }
}
