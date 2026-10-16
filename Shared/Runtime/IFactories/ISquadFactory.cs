using Delta.ECS;
using SlyPirates.Shared.Core;
using SlyPirates.Shared;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Data;

namespace SlyPirates.Shared.IFactories
{
    public interface ISquadFactory : IFactory<Entity, (EntityParameters entityParameters, TeamId team)> { }
}
