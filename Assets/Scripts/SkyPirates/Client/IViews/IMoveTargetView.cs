#nullable enable
using DVG.Core;
using DVG.MathsOld;

namespace DVG.SkyPirates.Client.IViews
{
    public interface IMoveTargetView : IView
    {
        public float3 Position { get; }
        public angle Rotation { get; }

        public float2 Direction { set; }
    }
}
