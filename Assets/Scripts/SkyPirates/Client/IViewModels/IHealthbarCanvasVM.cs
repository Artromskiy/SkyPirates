using DVG.Core;
using System;

namespace DVG.SkyPirates.Client.IViewModels
{
    public interface IHealthbarCanvasVM : IViewModel
    {
        void RegisterView(IView view);
        event Action<IView> OnViewCreated;
    }
}
