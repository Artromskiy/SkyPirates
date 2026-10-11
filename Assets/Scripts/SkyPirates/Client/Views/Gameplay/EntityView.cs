using Delta.ECS;
using Delta.ECS.Unity;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.IViews;
using DVG.SkyPirates.Client.Systems;
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

        public override void OnInject()
        {
            var componentViews = GetComponents<ComponentView>();

            foreach (var componentView in componentViews)
                componentView.ViewModel = ViewModel;
            foreach (var componentView in componentViews)
                componentView.OnPostInject();
        }

        private void Update()
        {
            if (ViewModel == null)
                return;

            if (ViewModel.Disposed)
            {
                foreach (var componentView in GetComponents<ComponentView>())
                    componentView.Dispose();
                Destroy(gameObject);
                return;
            }

            bool disabled = ViewModel.Disabled;
            if (_disabled == disabled && disabled)
                return;
            _disabled = disabled;


            _disposingTime = ViewModel.Alive ? 0 : _disposingTime + Time.unscaledDeltaTime;
            bool disposing = _disposingTime > _disposingDelay;

            if (_disposing == disposing && disposing)
                return;
            _disposing = disposing;

            foreach (var componentView in GetComponents<ComponentView>())
                componentView.Tick();
        }

        private void Start()
        {
            if (ViewModel != null || !this.TryGetEntity(out EntityContext context) || !(context.World is World world))
            {
                return;
            }

            if (EntityViewModelRegistry.TryGet(world, context.Entity, out var viewModel))
            {
                ViewModel = viewModel;
            }
        }

        private void Awake() => EditorChanges.SubscribeOnDestroy(gameObject, Dispose);
        private void Dispose()
        {
            if (this.TryGetEntity(out EntityContext context) && context.World is World world)
            {
                EntityViewModelRegistry.Remove(world, context.Entity);
            }

            ViewModel?.Dispose();
        }
    }
}
