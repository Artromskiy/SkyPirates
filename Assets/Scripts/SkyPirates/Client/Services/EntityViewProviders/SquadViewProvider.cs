using DVG.Core;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Shared.Components.Runtime;

namespace DVG.SkyPirates.Client.Services.EntityViewProviders
{
    public class SquadViewProvider : IEntityViewProvider
    {
        private readonly IPathViewFactory<IView<IEntityVM>> _pathViewFactory;
        private const string Path = "Prefabs/Squad/SquadView";

        public SquadViewProvider(IPathViewFactory<IView<IEntityVM>> pathViewFactory)
        {
            _pathViewFactory = pathViewFactory;
        }

        public bool TryCreateView(IEntityVM viewModel, out IView<IEntityVM> view)
        {
            view = default;
            if (!viewModel.Has<Squad>())
                return false;
            view = _pathViewFactory.Create(Path);
            return true;
        }
    }
}
