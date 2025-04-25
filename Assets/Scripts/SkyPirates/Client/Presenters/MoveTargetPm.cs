using DVG.Core;
using DVG.Maths;
using DVG.SkyPirates.Client.IViews;

namespace DVG.SkyPirates.Client.Presenters
{
    public class MoveTargetPm : Presenter<IMoveTargetView, object>
    {
        const float Speed = 7;
        public vec3 Position => View.Position;
        public angle Rotation => View.Rotation;

        public vec2 Direction { set => View.Direction = value * Speed; }

        public MoveTargetPm(IMoveTargetView view) : base(view, null) { }
    }
}
