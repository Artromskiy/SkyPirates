using System.Collections.Generic;
using UnityEngine;

namespace DVG.SkyPirates.Rendering.SphereOverlay.Internals
{
    internal static class SphereOverlayRegistry
    {
        internal const int MaximumVolumes = 32;
        internal const int GradientResolution = 128;

        internal struct Volume
        {
            public Object Owner;
            public Vector4 CenterRadius;
            public Gradient Gradient;
            public Color Tint;
        }

        private static readonly Dictionary<Object, Volume> Volumes = new Dictionary<Object, Volume>();
        private static readonly List<Object> StaleOwners = new List<Object>();

        internal static void Set(Object owner, Vector3 center, float radius, Gradient gradient, Color tint)
        {
            if (owner == null)
                return;

            Volumes[owner] = new Volume
            {
                Owner = owner,
                CenterRadius = new Vector4(center.x, center.y, center.z, Mathf.Max(0f, radius)),
                Gradient = gradient,
                Tint = tint
            };
        }

        internal static void Remove(Object owner)
        {
            if (owner != null)
                Volumes.Remove(owner);
        }

        internal static int CopyTo(Vector4[] centers, Color32[] gradientPixels)
        {
            StaleOwners.Clear();
            var count = 0;
            foreach (var pair in Volumes)
            {
                var volume = pair.Value;
                if (volume.Owner == null)
                {
                    StaleOwners.Add(pair.Key);
                    continue;
                }

                if (count >= centers.Length || (count + 1) * GradientResolution > gradientPixels.Length)
                    break;

                centers[count] = volume.CenterRadius;
                var gradient = volume.Gradient;
                var rowOffset = count * GradientResolution;
                for (var sample = 0; sample < GradientResolution; sample++)
                {
                    var t = sample / (float)(GradientResolution - 1);
                    gradientPixels[rowOffset + sample] = gradient != null
                        ? (Color32)(gradient.Evaluate(t) * volume.Tint)
                        : new Color32(255, 255, 255, 255);
                }
                count++;
            }

            for (var i = 0; i < StaleOwners.Count; i++)
                Volumes.Remove(StaleOwners[i]);

            return count;
        }
    }
}
