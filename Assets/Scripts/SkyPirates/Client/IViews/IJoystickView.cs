using DVG.Core;
using DVG.Maths;

namespace DVG.SkyPirates.Client.IViews
{
    public interface IJoystickView : IView
    {
        public vec2 Direction { get; }
        public bool Fixation { get; }
    }
}
