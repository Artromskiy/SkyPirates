using DVG.Json;
using DVG.MathsOld;
using System;

namespace DVG.SkyPirates.Shared.Models
{
    [JsonAsset]
    [Serializable]
    public partial class PackedCirclesModel
    {
        public float radius;
        public vec2[] points;

        public PackedCirclesModel(float radius, vec2[] points)
        {
            this.radius = radius;
            this.points = points;
        }
    }
}
