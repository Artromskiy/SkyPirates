using Delta;
using DVG.SkyPirates.Shared.Core;
using DVG.SkyPirates.Shared;

namespace DVG.SkyPirates.Client.IViewModels
{
    public interface ICameraVM : IViewModel
    {
        float3 TargetPosition { get; }
        float TargetDistance { get; }
        float TargetFov { get; }
        float TargetAngle { get; }
        float SmoothMoveTime { get; }
    }
}
