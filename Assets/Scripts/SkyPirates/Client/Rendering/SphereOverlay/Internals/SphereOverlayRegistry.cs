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
            public Vector4 Color;
        }

        private static readonly Dictionary<Object, Volume> Volumes = new Dictionary<Object, Volume>();
        private static readonly List<Object> StaleOwners = new List<Object>();

        internal static void Set(Object owner, Vector3 center, float radius, Color color)
        {
            if (owner == null)
                return;

            Volumes[owner] = new Volume
            {
                Owner = owner,
                CenterRadius = new Vector4(center.x, center.y, center.z, Mathf.Max(0f, radius)),
                Color = new Vector4(color.r, color.g, color.b, color.a)
            };
        }

        internal static void Remove(Object owner)
        {
            if (owner != null)
                Volumes.Remove(owner);
        }

        internal static int CopyTo(Vector4[] centers, Vector4[] colors)
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

                if (count >= centers.Length || count >= colors.Length)
                    break;

                centers[count] = volume.CenterRadius;
                colors[count] = volume.Color;
                count++;
            }

            for (var i = 0; i < StaleOwners.Count; i++)
                Volumes.Remove(StaleOwners[i]);

            return count;
        }
    }
}
