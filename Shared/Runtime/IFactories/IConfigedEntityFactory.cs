using Delta.ECS;
using SkyPirates.Shared.Core;
using SkyPirates.Shared;
using SkyPirates.Shared.Core.Ids;
using SkyPirates.Shared.Data;
using System;

namespace SkyPirates.Shared.IFactories
{
    public interface IConfigedEntityFactory<T> : IFactory<Entity, (T Id, EntityParameters parameters)> where T : struct, IId, IEquatable<T> { }
}
