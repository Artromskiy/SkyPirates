using DVG.SkyPirates.Shared.Core;
using DVG.SkyPirates.Shared.Core.Ids;
using DVG.SkyPirates.Shared.Data;
using System;

namespace DVG.SkyPirates.Shared.IFactories
{
    public interface IEntityConfigFactory<T> : IFactory<ComponentDataSet, T> where T : struct, IId, IEquatable<T> { }
}
