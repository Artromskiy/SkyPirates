using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Common.Rendering.HexTerrainStencilShadow
{
    /// <summary>
    /// A closed mesh with independent face vertices and degenerate bridges between adjacent faces.
    /// The vertex shader moves back faces away from the light, opening only silhouette bridges.
    /// See https://github.com/rhedgeco/UnityShadowVolumeGenerator for the volume construction.
    /// </summary>
    public static class HexTerrainShadowMesh
    {
        public static Mesh Create(Mesh source)
        {
            Vector3[] positions = source.vertices;
            int[] sourceIndices = source.triangles;
            var vertices = new List<Vector3>(sourceIndices.Length);
            var normals = new List<Vector3>(sourceIndices.Length);
            var indices = new List<int>(sourceIndices.Length * 4);
            var vertexIds = new Dictionary<Vector3Int, int>();
            var canonical = new List<Vector3>();
            var welded = new int[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 p = positions[i] * 100000f;
                var key = new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));
                if (!vertexIds.TryGetValue(key, out int id))
                {
                    id = vertexIds.Count;
                    vertexIds.Add(key, id);
                    canonical.Add(positions[i]);
                }
                welded[i] = id;
                positions[i] = canonical[id];
            }

            var edges = new Dictionary<(int, int), (int A, int B)>();
            var paired = new HashSet<(int, int)>();
            for (int i = 0; i < sourceIndices.Length; i += 3)
            {
                Vector3 a = positions[sourceIndices[i]];
                Vector3 b = positions[sourceIndices[i + 1]];
                Vector3 c = positions[sourceIndices[i + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a);
                if (normal.sqrMagnitude < 1e-12f)
                    throw new InvalidOperationException($"Shadow caster '{source.name}' contains a degenerate triangle.");
                normal.Normalize();
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                for (int j = 0; j < 3; j++)
                {
                    normals.Add(normal);
                    indices.Add(i + j);
                }
                Connect(i, i + 1, welded[sourceIndices[i]], welded[sourceIndices[i + 1]]);
                Connect(i + 1, i + 2, welded[sourceIndices[i + 1]], welded[sourceIndices[i + 2]]);
                Connect(i + 2, i, welded[sourceIndices[i + 2]], welded[sourceIndices[i]]);
            }
            if (edges.Count != 0 || source.bounds.size.y <= Mathf.Epsilon)
                throw new InvalidOperationException($"Shadow caster '{source.name}' must be a closed manifold with nonzero height.");

            var mesh = new Mesh { name = source.name + " Shadow Volume Template" };
            mesh.indexFormat = vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(indices, 0);
            mesh.UploadMeshData(true);
            return mesh;

            void Connect(int a, int b, int idA, int idB)
            {
                var key = (Math.Min(idA, idB), Math.Max(idA, idB));
                if (paired.Contains(key))
                    throw new InvalidOperationException($"Shadow caster '{source.name}' has a non-manifold edge.");
                if (!edges.TryGetValue(key, out var other))
                {
                    edges.Add(key, (a, b));
                    return;
                }
                if (welded[sourceIndices[other.A]] != idB || welded[sourceIndices[other.B]] != idA)
                    throw new InvalidOperationException($"Shadow caster '{source.name}' has inconsistent winding.");

                // Opposite to each face edge: together with the two caps this is a closed volume.
                indices.Add(b); indices.Add(a); indices.Add(other.A);
                indices.Add(other.B); indices.Add(other.A); indices.Add(a);
                edges.Remove(key);
                paired.Add(key);
            }
        }
    }
}
