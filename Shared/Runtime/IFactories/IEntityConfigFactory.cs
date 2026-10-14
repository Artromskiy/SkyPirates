using SkyPirates.Shared.Core;
using SkyPirates.Shared;
using SkyPirates.Shared.Core.Ids;
using SkyPirates.Shared.Data;
using System;

namespace SkyPirates.Shared.IFactories
{
    public interface IEntityConfigFactory<T> : IFactory<ComponentDataSet, T> where T : struct, IId, IEquatable<T> { }
}
