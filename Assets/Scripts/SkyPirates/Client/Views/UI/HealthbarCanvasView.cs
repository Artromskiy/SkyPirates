using DVG.Core;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.IViews;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views
{
    public class HealthbarCanvasView : View<IHealthbarCanvasVM>
    {
        public override void OnInject()
        {
            ViewModel.OnViewCreated += Register;
        }

        public void Register(IView healthbarView)
        {
            if (healthbarView is MonoBehaviour mono)
                mono.transform.SetParent(transform, false);
        }
    }
}