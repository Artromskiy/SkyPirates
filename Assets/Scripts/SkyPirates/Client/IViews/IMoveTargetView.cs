using DVG.Core;
using DVG.Maths;

namespace DVG.SkyPirates.Client.IViews
{
    public interface IMoveTargetView : IView
    {
        public vec3 Position { get; }
        public angle Rotation { get; }

        public vec2 Direction { set; }
    }
}
