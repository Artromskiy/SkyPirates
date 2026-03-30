using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.IViews;
using DVG.SkyPirates.Client.Views.Components;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Gameplay
{
    public class EntityView : View<IEntityVM>
    {
        [SerializeField]
        private float _disposingDelay = LerpConstants.AnimationTime;
        private float _disposingTime = 0;

        private bool _disabled;
        private bool _disposing;

        private ComponentView[] _componentViews;
        public override void OnInject()
        {
            _componentViews = GetComponents<ComponentView>();

            foreach (var componentView in _componentViews)
                componentView.ViewModel = ViewModel;
            foreach (var componentView in _componentViews)
                componentView.OnPostInject();
        }

        private void Update()
        {
            if (ViewModel.Disposed)
            {
                foreach (var componentView in _componentViews)
                    componentView.Dispose();
                Destroy(gameObject);
                return;
            }

            var disabled = ViewModel.Disabled;
            if (_disabled == disabled && disabled)
                return;
            _disabled = disabled;


            _disposingTime = ViewModel.Alive ? 0 : _disposingTime + Time.unscaledDeltaTime;
            var disposing = _disposingTime > _disposingDelay;

            if (_disposing == disposing && disposing)
                return;
            _disposing = disposing;

            foreach (var componentView in _componentViews)
                componentView.Tick();
        }

        private void Awake() => EditorChanges.SubscribeOnDestroy(gameObject, Dispose);
        private void Dispose() => ViewModel?.Dispose();
    }
}