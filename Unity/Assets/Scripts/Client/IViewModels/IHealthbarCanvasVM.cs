using SlyPirates.Shared.Core;
using System;
using UnityEngine;

namespace SlyPirates.Client.IViewModels
{
    public interface IHealthbarCanvasVM : IViewModel
    {
        void RegisterView(Transform view);
        event Action<Transform> OnViewCreated;
    }
}
