using DVG.SkyPirates.Client.IViewModels;
using System;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public abstract class ComponentView : MonoBehaviour, IDisposable
    {
        private string _debuggerName;
        public string DebuggerName => _debuggerName ??= GetType().Name;

        private IEntityVM _viewModel;
        public IEntityVM ViewModel
        {
            get => _viewModel;
            set
            {
                _viewModel = value;
                OnInject();
            }
        }

        public abstract void OnInject();
        public virtual void OnPostInject() { }
        public virtual void Tick() { }

        public virtual void Dispose() { }
        private void OnDestroy() => Dispose();

        protected event Action OnEditorChanged
        {
            add => EditorChanges.SubscribeOnChanged(this, value);
            remove => EditorChanges.UnsubscribeOnChanged(this, value);
        }
    }
}
