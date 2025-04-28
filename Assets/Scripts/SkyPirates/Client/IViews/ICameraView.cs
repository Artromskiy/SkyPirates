using DVG.Core;
using DVG.MathsOld;

namespace DVG.SkyPirates.Client.IViews
{
    public interface ICameraView : IView
    {
        public void SetData(float3 camPosition, float xRotation, float camFov, float3 listenerPosition);
    }
}
