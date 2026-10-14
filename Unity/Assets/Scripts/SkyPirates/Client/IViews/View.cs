using DVG.Core;
using UnityEngine;

namespace DVG.SkyPirates.Client.IViews
{
    [SelectionBase]
    public abstract class View<T> : MonoBehaviour,
        IView<T> where T : IViewModel
    {
        private T _viewModel;
        public T ViewModel
        {
            get => _viewModel;
            set
            {
                _viewModel = value;
                OnInject();
            }
        }
        public abstract void OnInject();
    }
}
