using Delta.ECS;
using System;
using SlyPirates.Shared.Core;
using SlyPirates.Shared.Data;

namespace SlyPirates.Shared.IFactories
{
    public interface IEntityFactory : IFactory<Entity, EntityParameters>
    {
        Entity Create(EntityParameters parameters, ReadOnlySpan<ComponentId> components);
    }
}
