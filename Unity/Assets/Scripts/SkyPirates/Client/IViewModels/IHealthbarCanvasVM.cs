using DVG.Core;
using System;
using UnityEngine;

namespace DVG.SkyPirates.Client.IViewModels
{
    public interface IHealthbarCanvasVM : IViewModel
    {
        void RegisterView(Transform view);
        event Action<Transform> OnViewCreated;
    }
}
