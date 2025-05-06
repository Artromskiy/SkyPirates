using DVG.Core;
using DVG.SkyPirates.OldShared.Ids;
using DVG.SkyPirates.OldShared.IViews;

namespace DVG.SkyPirates.Server.IFactories
{
    public interface IUnitViewFactory : IFactory<IUnitView, (UnitId unitId, int level, int merge)> { }
}
