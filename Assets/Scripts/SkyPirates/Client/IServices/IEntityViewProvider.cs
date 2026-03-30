using DVG.Core;
using DVG.SkyPirates.Client.IViewModels;

namespace DVG.SkyPirates.Client.IServices
{
    public interface IEntityViewProvider
    {
        bool TryCreateView(IEntityVM viewModel, out IView<IEntityVM> view);
    }
}
