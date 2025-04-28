using DVG.Json;
using System;

namespace DVG.SkyPirates.Shared.Models
{
    [JsonAsset]
    [Serializable]
    public partial class PackedCirclesModel
    {
        public float radius;
        public float2[] points;

        public PackedCirclesModel(float radius, float2[] points)
        {
            this.radius = radius;
            this.points = points;
        }
    }
}
