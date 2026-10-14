using Delta.ECS;
using SkyPirates.Shared.Core;
using SkyPirates.Shared;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.Data;

namespace SkyPirates.Shared.IFactories
{
    public interface ISquadFactory : IFactory<Entity, (EntityParameters entityParameters, TeamId team)> { }
}
