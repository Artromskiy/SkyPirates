using DVG.Core;
using DVG.Maths;

namespace DVG.SkyPirates.Shared.IViews
{
    public interface IUnitView : IView
    {
        public vec2 Velocity { set; }
        public angle Rotation { set; }

        public vec3 Position { get; }
    }
}
