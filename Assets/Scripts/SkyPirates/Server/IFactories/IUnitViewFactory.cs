using DVG.Core;
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.IViews;

namespace DVG.SkyPirates.Server.IFactories
{
    public interface IUnitViewFactory : IFactory<IUnitView, (UnitId unitId, int level, int merge)> { }
}
