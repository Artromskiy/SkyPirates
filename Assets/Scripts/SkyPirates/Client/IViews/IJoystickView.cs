using DVG.Core;

namespace DVG.SkyPirates.Client.IViews
{
    public interface IJoystickView : IView
    {
        public float2 Direction { get; }
        public bool Fixation { get; }
    }
}
