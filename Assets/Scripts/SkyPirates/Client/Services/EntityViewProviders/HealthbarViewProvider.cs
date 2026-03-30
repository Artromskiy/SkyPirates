using DVG.Core;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Runtime;

namespace DVG.SkyPirates.Client.Services.EntityViewProviders
{
    public class HealthbarViewProvider : IEntityViewProvider
    {
        private readonly IHealthbarCanvasVM _healthbarCanvas;
        private readonly IPathViewFactory<IView<IEntityVM>> _pathViewFactory;

        private const string Path = "Prefabs/UI/HealthbarView";

        public HealthbarViewProvider(IHealthbarCanvasVM healthbarCanvas, IPathViewFactory<IView<IEntityVM>> pathViewFactory)
        {
            _healthbarCanvas = healthbarCanvas;
            _pathViewFactory = pathViewFactory;
        }

        public bool TryCreateView(IEntityVM viewModel, out IView<IEntityVM> view)
        {
            view = default;
            if (!HasNeededComponents(viewModel))
                return false;
            view = _pathViewFactory.Create(Path);
            _healthbarCanvas.RegisterView(view);
            return true;
        }

        private bool HasNeededComponents(IEntityVM viewModel) =>
            viewModel.Has<Health>() && viewModel.Has<MaxHealth>() && viewModel.Has<Position>();
    }
}
