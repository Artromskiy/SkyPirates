using DVG.Core;
using DVG.MathsOld;

namespace DVG.SkyPirates.Client.IViews
{
    public interface ICameraView : IView
    {
        public void SetData(vec3 camPosition, quat camRotation, float camFov, vec3 listenerPosition);
    }
}
