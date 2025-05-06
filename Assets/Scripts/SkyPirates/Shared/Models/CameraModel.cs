using DVG.Json;
using System;

namespace DVG.SkyPirates.OldShared.Models
{
    [JsonAsset]
    [Serializable]
    public partial class CameraModel
    {
        public float minXAngle;
        public float maxXAngle;
        public float minFov;
        public float maxFov;
        public float minDistance;
        public float maxDistance;
        public float smoothMoveTime;
    }
}