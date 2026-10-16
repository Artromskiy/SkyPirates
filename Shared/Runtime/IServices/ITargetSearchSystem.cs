using Delta.ECS;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace SlyPirates.Shared.IServices
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
            List<SyncId> targets);
    }
}
