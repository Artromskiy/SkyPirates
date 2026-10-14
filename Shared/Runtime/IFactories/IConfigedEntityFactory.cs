using Delta.ECS;
using DVG.Core;
using DVG.Ids;
using DVG.SkyPirates.Shared.Data;
using System;

namespace DVG.SkyPirates.Shared.IFactories
{
    public interface IConfigedEntityFactory<T> : IFactory<Entity, (T Id, EntityParameters parameters)> where T : struct, IId, IEquatable<T> { }
}
