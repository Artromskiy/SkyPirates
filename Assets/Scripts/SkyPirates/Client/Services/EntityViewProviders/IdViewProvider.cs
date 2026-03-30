using DVG.Core;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Shared.Ids;
using System;

namespace DVG.SkyPirates.Client.Services.EntityViewProviders
{
    public class IdViewProvider : IEntityViewProvider
    {
        private readonly IPathViewFactory<IView<IEntityVM>> _pathViewFactory;

        public IdViewProvider(IPathViewFactory<IView<IEntityVM>> pathViewFactory)
        {
            _pathViewFactory = pathViewFactory;
        }

        public bool TryCreateView(IEntityVM viewModel, out IView<IEntityVM> view)
        {
            view = default;
            if (!HasIdView(viewModel))
                return false;

            view = _pathViewFactory.Create(GetIdViewPath(viewModel));
            return true;
        }

        private bool HasIdView(IEntityVM viewModel) =>
                viewModel.Has<UnitId>() ||
                viewModel.Has<TreeId>() ||
                viewModel.Has<RockId>() ||
                viewModel.Has<GoodsId>() ||
                viewModel.Has<CactusId>();

        private string GetIdViewPath(IEntityVM viewModel) =>
            viewModel.Has<UnitId>() ? IdPathFormatter.FormatViewPath<UnitId>() :
            viewModel.Has<TreeId>() ? IdPathFormatter.FormatViewPath<TreeId>() :
            viewModel.Has<RockId>() ? IdPathFormatter.FormatViewPath<RockId>() :
            viewModel.Has<GoodsId>() ? IdPathFormatter.FormatViewPath<GoodsId>() :
            viewModel.Has<CactusId>() ? IdPathFormatter.FormatViewPath<CactusId>() :
            throw new InvalidOperationException();

    }
}
