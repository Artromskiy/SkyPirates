using Delta;
using SlyPirates.Shared.Core;
using SlyPirates.Shared;

namespace SlyPirates.Client.IViewModels
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
