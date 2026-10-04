using System.Collections.Generic;
using UnityEngine;

namespace DVG.SkyPirates.Rendering.SphereOverlay.Internals
{
    internal static class SphereOverlayRegistry
    {
        internal const int MaximumVolumes = 32;

        internal struct Volume
        {
            public Object Owner;
            public Vector4 CenterRadius;
            public Vector4 CenterColor;
            public Vector4 EdgeColor;
        }

        private static readonly Dictionary<Object, Volume> Volumes = new Dictionary<Object, Volume>();
        private static readonly List<Object> StaleOwners = new List<Object>();

        internal static void Set(Object owner, Vector3 center, float radius, Color color, Color centerColor, Color edgeColor)
        {
            if (owner == null)
                return;

            centerColor *= color;
            edgeColor *= color;

            Volumes[owner] = new Volume
            {
                Owner = owner,
                CenterRadius = new Vector4(center.x, center.y, center.z, Mathf.Max(0f, radius)),
                CenterColor = new Vector4(centerColor.r, centerColor.g, centerColor.b, centerColor.a),
                EdgeColor = new Vector4(edgeColor.r, edgeColor.g, edgeColor.b, edgeColor.a)
            };
        }

        internal static void Remove(Object owner)
        {
            if (owner != null)
                Volumes.Remove(owner);
        }

        internal static int CopyTo(Vector4[] centers, Vector4[] centerColors, Vector4[] edgeColors)
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

                if (count >= centers.Length || count >= centerColors.Length || count >= edgeColors.Length)
                    break;

                centers[count] = volume.CenterRadius;
                centerColors[count] = volume.CenterColor;
                edgeColors[count] = volume.EdgeColor;
                count++;
            }

            for (var i = 0; i < StaleOwners.Count; i++)
                Volumes.Remove(StaleOwners[i]);

            return count;
        }
    }
}
