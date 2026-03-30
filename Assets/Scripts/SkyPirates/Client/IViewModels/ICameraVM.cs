using DVG.Core;

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
