using SkyPirates.Shared.Core;
using SkyPirates.Shared;
using System;
using UnityEngine;

namespace SkyPirates.Client.IViewModels
{
    public interface IHealthbarCanvasVM : IViewModel
    {
        void RegisterView(Transform view);
        event Action<Transform> OnViewCreated;
    }
}
