using DVG.Core;
using DVG.SkyPirates.Server.Presenters;
using DVG.SkyPirates.Shared.Ids;

namespace DVG.SkyPirates.Server.IFactories
{
    public interface IUnitFactory : IFactory<UnitPm, (UnitId unitId, int level, int merge)> { }
}
