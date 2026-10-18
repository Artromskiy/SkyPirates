using System.Collections.Generic;
using UnityEngine;

namespace Common.Rendering.HexTerrainStencilShadow
{
    /// <summary>Chunk-owned instance data. Registration changes only when terrain changes.</summary>
    public static class HexTerrainStencilShadowRegistry
    {
        private static readonly Dictionary<object, Entry> Entries = new();

        internal static int Version { get; private set; }
        internal static Dictionary<object, Entry>.ValueCollection Values => Entries.Values;

        public static void Set(object owner, Mesh mesh, List<Matrix4x4> instances)
        {
            if (instances.Count == 0)
            {
                Remove(owner);
                return;
            }
            var data = new Instance[instances.Count];
            Bounds bounds = TransformBounds(mesh.bounds, instances[0]);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new Instance(instances[i]);
                bounds.Encapsulate(TransformBounds(mesh.bounds, instances[i]));
            }
            Entries[owner] = new Entry(mesh, data, bounds);
            Version++;
        }

        public static void Remove(object owner)
        {
            if (Entries.Remove(owner)) Version++;
        }

        internal readonly struct Entry
        {
            internal readonly Mesh Mesh;
            internal readonly Instance[] Instances;
            internal readonly Bounds Bounds;

            internal Entry(Mesh mesh, Instance[] instances, Bounds bounds)
            {
                Mesh = mesh;
                Instances = instances;
                Bounds = bounds;
            }
        }

        // Matches ShadowInstance in the shader. Build inverses only for changed chunks.
        internal readonly struct Instance
        {
            public readonly Matrix4x4 ObjectToWorld;
            public readonly Matrix4x4 WorldToObject;
            internal Instance(Matrix4x4 matrix) { ObjectToWorld = matrix; WorldToObject = matrix.inverse; }
        }

        private static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
        {
            Vector3 x = matrix.MultiplyVector(new Vector3(bounds.extents.x, 0f, 0f));
            Vector3 y = matrix.MultiplyVector(new Vector3(0f, bounds.extents.y, 0f));
            Vector3 z = matrix.MultiplyVector(new Vector3(0f, 0f, bounds.extents.z));
            return new Bounds(matrix.MultiplyPoint3x4(bounds.center), new Vector3(
                Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
                Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z)) * 2f);
        }
    }
}
