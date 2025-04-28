using DVG.Core;
using DVG.MathsOld;

namespace DVG.SkyPirates.Shared.IViews
{
    public interface IUnitView : IView
    {
        public float2 Velocity { set; }
        public angle Rotation { set; }

        public float3 Position { get; }
    }
}
