using Delta.ECS;
using SkyPirates.Shared.Components.Config;
using SkyPirates.Shared.Components.Framed;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace SkyPirates.Shared.IServices
{
    public interface ITargetSearchSystem : IDeltaTickableExecutor
    {
        Entity? FindTarget(
            in Position position,
            in TargetSearchDistance searchDistance,
            in TargetSearchPosition searchPosition,
            in TeamId team);

        void FindTargets(
            in TargetSearchDistance searchDistance,
            in TargetSearchPosition searchPosition,
            in TeamId team,
            List<Entity> targets);
    }
}
