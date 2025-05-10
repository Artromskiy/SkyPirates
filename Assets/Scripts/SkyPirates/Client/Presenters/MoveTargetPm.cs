#nullable enable
using DVG.Core;
using DVG.MathsOld;
using DVG.SkyPirates.Client.IViews;

namespace DVG.SkyPirates.Client.Presenters
{
    public class MoveTargetPm : Presenter<IMoveTargetView, object>
    {
        private const float Speed = 7;
        public float3 Position => View.Position;
        public angle Rotation => View.Rotation;

        public float2 Direction { set => View.Direction = value * Speed; }

        public MoveTargetPm(IMoveTargetView view) : base(view, null) { }
    }
}
