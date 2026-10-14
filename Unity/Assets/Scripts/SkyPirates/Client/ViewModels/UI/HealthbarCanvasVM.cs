using SkyPirates.Client.IViewModels;
using System;
using UnityEngine;

namespace SkyPirates.Client.ViewModels.UI
{
    public class HealthbarCanvasVM : IHealthbarCanvasVM
    {
        public event Action<Transform> OnViewCreated;
        public void RegisterView(Transform view) => OnViewCreated?.Invoke(view);
    }
}
