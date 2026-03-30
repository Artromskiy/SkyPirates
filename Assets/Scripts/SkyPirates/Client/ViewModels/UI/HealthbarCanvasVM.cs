using DVG.Core;
using DVG.SkyPirates.Client.IViewModels;
using System;

namespace DVG.SkyPirates.Client.ViewModels.UI
{
    public class HealthbarCanvasVM : IHealthbarCanvasVM
    {
        public event Action<IView> OnViewCreated;
        public void RegisterView(IView view) => OnViewCreated?.Invoke(view);
    }
}
