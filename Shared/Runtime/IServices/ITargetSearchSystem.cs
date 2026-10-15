using Delta.ECS;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.IServices
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
