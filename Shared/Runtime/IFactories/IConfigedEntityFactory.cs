using Delta.ECS;
using SlyPirates.Shared.Core;
using SlyPirates.Shared.Core.Ids;
using SlyPirates.Shared.Data;
using System;

namespace SlyPirates.Shared.IFactories
{
    public interface IConfigedEntityFactory<T> : IFactory<Entity, (T Id, EntityParameters parameters)> where T : struct, IId, IEquatable<T> { }
}
