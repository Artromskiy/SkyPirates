using DVG.Core;
using DVG.SkyPirates.OldShared.Ids;
using DVG.SkyPirates.OldShared.Models;

namespace DVG.SkyPirates.Server.IFactories
{
    public interface IUnitModelFactory : IFactory<UnitModel, (UnitId unitId, int level, int merge)> { }
}
