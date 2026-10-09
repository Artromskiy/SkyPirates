using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DVG.SkyPirates.Tooling.MeshColorFilter.Editor
{
    public static class MeshColorFilterProcessor
    {
        private sealed class Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector4 Tangent;
            public Color Color;
            public readonly Vector4[] Uv = new Vector4[8];
            public Dictionary<int, float> BoneWeights = new Dictionary<int, float>();
            public Vector3[][] BlendVertices;
            public Vector3[][] BlendNormals;
            public Vector3[][] BlendTangents;
        }

        private sealed class OutputGroup
        {
            public Material Material;
            public int OriginalSubmesh = -1;
            public readonly List<Vertex> Vertices = new List<Vertex>();
            public readonly List<int> Indices = new List<int>();
        }

        public static bool TryCreatePrefab(GameObject sourceAsset, Renderer selectedRenderer, MeshColorFilterProfile profile, out string resultPath, out string message)
        {
            resultPath = null;
            message = null;
            if (sourceAsset == null || selectedRenderer == null || profile == null)
            {
                message = "Choose a source, renderer, and filter profile.";
                return false;
            }
            if (profile.Rules == null || profile.Rules.Any(rule => rule == null || rule.Enabled && rule.Material == null))
            {
                message = "Assign a material to every enabled color rule before processing.";
                return false;
            }

            string sourcePath = AssetDatabase.GetAssetPath(sourceAsset);
            if (string.IsNullOrEmpty(sourcePath) || (!PrefabUtility.IsPartOfPrefabAsset(sourceAsset) && !AssetImporter.GetAtPath(sourcePath)))
            {
                message = "The source must be a prefab or an imported model asset.";
                return false;
            }

            string rendererPath = GetHierarchyPath(selectedRenderer.transform, sourceAsset.transform);
            if (rendererPath == null)
            {
                message = "The selected renderer does not belong to the selected source asset.";
                return false;
            }

            GameObject contents = null;
            Mesh generatedMesh = null;
            string meshPath = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(sourcePath);
                Transform rendererTransform = FindByPath(contents.transform, rendererPath);
                Renderer targetRenderer = rendererTransform == null ? null : rendererTransform.GetComponent<Renderer>();
                if (targetRenderer == null || !(targetRenderer is MeshRenderer || targetRenderer is SkinnedMeshRenderer))
                {
                    message = "The selected MeshRenderer or SkinnedMeshRenderer could not be resolved in the loaded asset.";
                    return false;
                }

                Mesh sourceMesh = targetRenderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : rendererTransform.GetComponent<MeshFilter>()?.sharedMesh;
                if (sourceMesh == null)
                {
                    message = "The selected renderer has no mesh.";
                    return false;
                }

                if (!TryBuildMesh(sourceMesh, targetRenderer.sharedMaterials, profile, out generatedMesh, out Material[] materials, out message))
                    return false;

                string outputFolder = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(outputFolder) || !outputFolder.Equals("Assets") && !outputFolder.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    message = "The source must be inside the Assets folder so the generated prefab and mesh can be saved beside it.";
                    return false;
                }
                string safeName = MakeSafeFileName(sourceAsset.name) + "_Filtered";
                resultPath = AssetDatabase.GenerateUniqueAssetPath(outputFolder + "/" + safeName + ".prefab");

                string folder = Path.GetDirectoryName(resultPath)?.Replace('\\', '/') ?? "Assets";
                meshPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + Path.GetFileNameWithoutExtension(resultPath) + "_Mesh.asset");
                generatedMesh.name = Path.GetFileNameWithoutExtension(meshPath);
                AssetDatabase.CreateAsset(generatedMesh, meshPath);
                generatedMesh = null;

                Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (savedMesh == null)
                    throw new InvalidOperationException("Unity did not create the output mesh asset.");
                if (targetRenderer is SkinnedMeshRenderer targetSkinned)
                {
                    targetSkinned.sharedMesh = savedMesh;
                }
                else
                {
                    rendererTransform.GetComponent<MeshFilter>().sharedMesh = savedMesh;
                }
                targetRenderer.sharedMaterials = materials;
                if (PrefabUtility.SaveAsPrefabAsset(contents, resultPath) == null)
                    throw new InvalidOperationException("Unity did not save the output prefab.");
                AssetDatabase.SaveAssets();
                message = "Created processed prefab and mesh.";
                return true;
            }
            catch (Exception exception)
            {
                if (!string.IsNullOrEmpty(resultPath))
                    AssetDatabase.DeleteAsset(resultPath);
                if (!string.IsNullOrEmpty(meshPath))
                    AssetDatabase.DeleteAsset(meshPath);
                resultPath = null;
                message = "Mesh processing failed: " + exception.Message;
                return false;
            }
            finally
            {
                if (generatedMesh != null)
                    UnityEngine.Object.DestroyImmediate(generatedMesh);
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        public static bool TryBuildMesh(Mesh source, Material[] sourceMaterials, MeshColorFilterProfile profile, out Mesh output, out Material[] outputMaterials, out string message)
        {
            return TryBuildMesh(source, sourceMaterials, profile, out output, out outputMaterials, out _, out message);
        }

        public static bool TryBuildMesh(Mesh source, Material[] sourceMaterials, MeshColorFilterProfile profile, out Mesh output, out Material[] outputMaterials, out int[] outputSubmeshRuleIndices, out string message)
        {
            output = null;
            outputMaterials = null;
            outputSubmeshRuleIndices = null;
            message = null;
            if (source == null || profile == null)
            {
                message = "Mesh and profile are required.";
                return false;
            }
            if (profile.Rules == null || profile.Rules.Count == 0)
            {
                message = "Add at least one color rule.";
                return false;
            }
            List<Vertex> vertices;
            List<List<int>> submeshIndices = new List<List<int>>();
            List<MeshTopology> topologies = new List<MeshTopology>();
            using (Mesh.MeshDataArray dataArray = MeshUtility.AcquireReadOnlyMeshData(source))
            {
                Mesh.MeshData data = dataArray[0];
                if (!data.HasVertexAttribute(VertexAttribute.Color))
                {
                    message = "The source mesh has no vertex colors. Add vertex colors in the source asset and try again.";
                    return false;
                }
                vertices = ReadVertices(source, data);

                for (int i = 0; i < data.subMeshCount; i++)
                {
                    SubMeshDescriptor descriptor = data.GetSubMesh(i);
                    var nativeIndices = new NativeArray<int>(descriptor.indexCount, Allocator.Temp);
                    data.GetIndices(nativeIndices, i, true);
                    submeshIndices.Add(nativeIndices.ToArray().ToList());
                    nativeIndices.Dispose();
                    topologies.Add(descriptor.topology);
                }
            }
            List<OutputGroup> groups = new List<OutputGroup>();
            for (int i = 0; i < profile.Rules.Count; i++)
                groups.Add(new OutputGroup { Material = profile.Rules[i]?.Material });
            List<OutputGroup> unmatched = new List<OutputGroup>();
            for (int i = 0; i < submeshIndices.Count; i++)
                unmatched.Add(new OutputGroup { OriginalSubmesh = i, Material = i < (sourceMaterials?.Length ?? 0) ? sourceMaterials[i] : null });

            int matchedTriangleCount = 0;
            for (int submesh = 0; submesh < submeshIndices.Count; submesh++)
            {
                List<int> indices = submeshIndices[submesh];
                if (topologies[submesh] != MeshTopology.Triangles)
                {
                    OutputGroup group = unmatched[submesh];
                    foreach (int index in indices)
                    {
                        group.Indices.Add(group.Vertices.Count);
                        group.Vertices.Add(vertices[index]);
                    }
                    continue;
                }
                if (indices.Count % 3 != 0)
                {
                    message = "A triangle submesh has an invalid index count.";
                    return false;
                }
                for (int i = 0; i < indices.Count; i += 3)
                {
                    Vertex a = vertices[indices[i]];
                    Vertex b = vertices[indices[i + 1]];
                    Vertex c = vertices[indices[i + 2]];
                    if (profile.TriangleMode == MeshTriangleAssignmentMode.AdaptiveSubdivision)
                    {
                        Subdivide(a, b, c, profile.AdaptiveSplitDepth, profile.Rules, groups, unmatched[submesh], ref matchedTriangleCount);
                    }
                    else
                    {
                        int rule = profile.TriangleMode == MeshTriangleAssignmentMode.MajorityVertices
                            ? MatchMajority(a.Color, b.Color, c.Color, profile.Rules)
                            : Match(Average(a.Color, b.Color, c.Color), profile.Rules);
                        AddTriangle(rule < 0 ? unmatched[submesh] : groups[rule], a, b, c);
                        if (rule >= 0)
                            matchedTriangleCount++;
                    }
                }
            }

            if (matchedTriangleCount == 0 || groups.All(group => group.Indices.Count == 0))
            {
                message = "No triangles matched the configured color rules. No output was created.";
                return false;
            }

            List<OutputGroup> usedGroups = groups.Where(group => group.Indices.Count > 0).Concat(unmatched.Where(group => group.Indices.Count > 0)).ToList();
            outputMaterials = usedGroups.Select(group => group.Material).ToArray();
            outputSubmeshRuleIndices = usedGroups.Select(group => groups.IndexOf(group)).ToArray();
            output = CreateMesh(source, usedGroups);
            return true;
        }

        private static List<Vertex> ReadVertices(Mesh mesh, Mesh.MeshData data)
        {
            int count = data.vertexCount;
            List<Vector3> positions = ReadAttribute<Vector3>(data, count, (meshData, array) => meshData.GetVertices(array));
            var normals = new List<Vector3>(count);
            if (data.HasVertexAttribute(VertexAttribute.Normal)) normals.AddRange(ReadAttribute<Vector3>(data, count, (meshData, array) => meshData.GetNormals(array)));
            var tangents = new List<Vector4>(count);
            if (data.HasVertexAttribute(VertexAttribute.Tangent)) tangents.AddRange(ReadAttribute<Vector4>(data, count, (meshData, array) => meshData.GetTangents(array)));
            var colors = new List<Color>(count);
            if (data.HasVertexAttribute(VertexAttribute.Color)) colors.AddRange(ReadAttribute<Color>(data, count, (meshData, array) => meshData.GetColors(array)));
            var uvs = new List<Vector4>[8];
            for (int channel = 0; channel < 8; channel++)
            {
                VertexAttribute attribute = (VertexAttribute)((int)VertexAttribute.TexCoord0 + channel);
                if (!data.HasVertexAttribute(attribute)) continue;
                uvs[channel] = ReadAttribute<Vector4>(data, count, (meshData, array) => meshData.GetUVs(channel, array));
            }

            var boneCounts = mesh.GetBonesPerVertex();
            var allBoneWeights = mesh.GetAllBoneWeights();
            var weights = new List<Dictionary<int, float>>(count);
            int weightIndex = 0;
            for (int i = 0; i < count; i++)
            {
                var influences = new Dictionary<int, float>();
                if (boneCounts.Length > i)
                    for (int j = 0; j < boneCounts[i]; j++)
                    {
                        BoneWeight1 influence = allBoneWeights[weightIndex++];
                        influences[influence.boneIndex] = influence.weight;
                    }
                weights.Add(influences);
            }
            boneCounts.Dispose();
            allBoneWeights.Dispose();
            var result = new List<Vertex>(count);
            for (int i = 0; i < count; i++)
            {
                var vertex = new Vertex
                {
                    Position = positions[i],
                    Normal = i < normals.Count ? normals[i] : Vector3.zero,
                    Tangent = i < tangents.Count ? tangents[i] : Vector4.zero,
                    Color = i < colors.Count ? colors[i] : default,
                    BoneWeights = weights[i],
                };
                for (int channel = 0; channel < 8; channel++)
                    if (uvs[channel] != null && i < uvs[channel].Count) vertex.Uv[channel] = uvs[channel][i];
                result.Add(vertex);
            }

            int shapeCount = mesh.blendShapeCount;
            if (shapeCount > 0)
            {
                for (int shape = 0; shape < shapeCount; shape++)
                {
                    int frameCount = mesh.GetBlendShapeFrameCount(shape);
                    var frameVertices = new Vector3[frameCount][];
                    var frameNormals = new Vector3[frameCount][];
                    var frameTangents = new Vector3[frameCount][];
                    for (int frame = 0; frame < frameCount; frame++)
                    {
                        frameVertices[frame] = new Vector3[count];
                        frameNormals[frame] = new Vector3[count];
                        frameTangents[frame] = new Vector3[count];
                        mesh.GetBlendShapeFrameVertices(shape, frame, frameVertices[frame], frameNormals[frame], frameTangents[frame]);
                    }
                    for (int i = 0; i < count; i++)
                    {
                        result[i].BlendVertices ??= new Vector3[shapeCount][];
                        result[i].BlendNormals ??= new Vector3[shapeCount][];
                        result[i].BlendTangents ??= new Vector3[shapeCount][];
                        result[i].BlendVertices[shape] = new Vector3[frameCount];
                        result[i].BlendNormals[shape] = new Vector3[frameCount];
                        result[i].BlendTangents[shape] = new Vector3[frameCount];
                        for (int frame = 0; frame < frameCount; frame++)
                        {
                            result[i].BlendVertices[shape][frame] = frameVertices[frame][i];
                            result[i].BlendNormals[shape][frame] = frameNormals[frame][i];
                            result[i].BlendTangents[shape][frame] = frameTangents[frame][i];
                        }
                    }
                }
            }
            return result;
        }

        private static List<T> ReadAttribute<T>(Mesh.MeshData data, int count, Action<Mesh.MeshData, NativeArray<T>> read) where T : struct
        {
            var values = new NativeArray<T>(count, Allocator.Temp);
            read(data, values);
            List<T> result = values.ToArray().ToList();
            values.Dispose();
            return result;
        }

        private static Mesh CreateMesh(Mesh source, List<OutputGroup> groups)
        {
            int count = groups.Sum(group => group.Vertices.Count);
            var mesh = new Mesh { name = source.name + "_Filtered" };
            mesh.indexFormat = count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.bindposes = source.bindposes;
            mesh.subMeshCount = groups.Count;
            bool hasNormals = source.HasVertexAttribute(VertexAttribute.Normal);
            bool hasTangents = source.HasVertexAttribute(VertexAttribute.Tangent);
            bool hasColors = true;
            bool hasBones = groups.SelectMany(group => group.Vertices).Any(vertex => vertex.BoneWeights.Count > 0);
            var positions = new List<Vector3>(count);
            var normals = new List<Vector3>(count);
            var tangents = new List<Vector4>(count);
            var colors = new List<Color>(count);
            var uv = Enumerable.Range(0, 8).Select(_ => new List<Vector4>(count)).ToArray();
            var weights = new List<Dictionary<int, float>>(count);
            var outputIndices = new List<int[]>(groups.Count);
            var outputTopologies = new List<MeshTopology>(groups.Count);
            int baseVertex = 0;
            for (int submesh = 0; submesh < groups.Count; submesh++)
            {
                OutputGroup group = groups[submesh];
                positions.AddRange(group.Vertices.Select(vertex => vertex.Position));
                if (hasNormals) normals.AddRange(group.Vertices.Select(vertex => vertex.Normal));
                if (hasTangents) tangents.AddRange(group.Vertices.Select(vertex => vertex.Tangent));
                if (hasColors) colors.AddRange(group.Vertices.Select(vertex => vertex.Color));
                if (hasBones) weights.AddRange(group.Vertices.Select(vertex => vertex.BoneWeights));
                for (int channel = 0; channel < 8; channel++) uv[channel].AddRange(group.Vertices.Select(vertex => vertex.Uv[channel]));
                outputIndices.Add(group.Indices.Select(index => index + baseVertex).ToArray());
                outputTopologies.Add(group.OriginalSubmesh >= 0 && group.OriginalSubmesh < source.subMeshCount ? source.GetTopology(group.OriginalSubmesh) : MeshTopology.Triangles);
                baseVertex += group.Vertices.Count;
            }
            mesh.SetVertices(positions);
            if (hasNormals) mesh.SetNormals(normals);
            if (hasTangents) mesh.SetTangents(tangents);
            if (hasColors) mesh.SetColors(colors);
            for (int channel = 0; channel < 8; channel++)
                if (source.HasVertexAttribute((VertexAttribute)((int)VertexAttribute.TexCoord0 + channel))) mesh.SetUVs(channel, uv[channel]);
            for (int submesh = 0; submesh < groups.Count; submesh++) mesh.SetIndices(outputIndices[submesh], outputTopologies[submesh], submesh, false);
            if (hasBones)
            {
                var counts = new NativeArray<byte>(count, Allocator.Temp);
                var flattened = new List<BoneWeight1>();
                for (int i = 0; i < weights.Count; i++)
                {
                    var ordered = weights[i].OrderByDescending(pair => pair.Value).ToArray();
                    counts[i] = (byte)Mathf.Min(ordered.Length, byte.MaxValue);
                    for (int j = 0; j < counts[i]; j++) flattened.Add(new BoneWeight1 { boneIndex = ordered[j].Key, weight = ordered[j].Value });
                }
                var weightArray = new NativeArray<BoneWeight1>(flattened.ToArray(), Allocator.Temp);
                mesh.SetBoneWeights(counts, weightArray);
                counts.Dispose();
                weightArray.Dispose();
            }

            if (source.blendShapeCount > 0)
            {
                for (int shape = 0; shape < source.blendShapeCount; shape++)
                {
                    string shapeName = source.GetBlendShapeName(shape);
                    int frameCount = source.GetBlendShapeFrameCount(shape);
                    for (int frame = 0; frame < frameCount; frame++)
                    {
                        var deltaVertices = new List<Vector3>(count);
                        var deltaNormals = new List<Vector3>(count);
                        var deltaTangents = new List<Vector3>(count);
                        foreach (OutputGroup group in groups)
                        foreach (Vertex vertex in group.Vertices)
                        {
                            deltaVertices.Add(vertex.BlendVertices[shape][frame]);
                            deltaNormals.Add(vertex.BlendNormals[shape][frame]);
                            deltaTangents.Add(vertex.BlendTangents[shape][frame]);
                        }
                        mesh.AddBlendShapeFrame(shapeName, source.GetBlendShapeFrameWeight(shape, frame), deltaVertices.ToArray(), deltaNormals.ToArray(), deltaTangents.ToArray());
                    }
                }
            }
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void Subdivide(Vertex a, Vertex b, Vertex c, int depth, List<MeshColorFilterRule> rules, List<OutputGroup> groups, OutputGroup unmatched, ref int matchedCount)
        {
            int ra = Match(a.Color, rules), rb = Match(b.Color, rules), rc = Match(c.Color, rules);
            if (ra == rb && rb == rc)
            {
                AddTriangle(ra < 0 ? unmatched : groups[ra], a, b, c);
                if (ra >= 0) matchedCount++;
                return;
            }
            if (depth <= 0)
            {
                int average = Match(Average(a.Color, b.Color, c.Color), rules);
                AddTriangle(average < 0 ? unmatched : groups[average], a, b, c);
                if (average >= 0) matchedCount++;
                return;
            }
            Vertex ab = Interpolate(a, b, 0.5f), bc = Interpolate(b, c, 0.5f), ca = Interpolate(c, a, 0.5f);
            Subdivide(a, ab, ca, depth - 1, rules, groups, unmatched, ref matchedCount);
            Subdivide(ab, b, bc, depth - 1, rules, groups, unmatched, ref matchedCount);
            Subdivide(ca, bc, c, depth - 1, rules, groups, unmatched, ref matchedCount);
            Subdivide(ab, bc, ca, depth - 1, rules, groups, unmatched, ref matchedCount);
        }

        private static Vertex Interpolate(Vertex a, Vertex b, float t)
        {
            var result = new Vertex
            {
                Position = Vector3.Lerp(a.Position, b.Position, t),
                Normal = Vector3.Slerp(a.Normal, b.Normal, t).normalized,
                Tangent = Vector4.Lerp(a.Tangent, b.Tangent, t),
                Color = Color.Lerp(a.Color, b.Color, t),
                BoneWeights = InterpolateWeight(a.BoneWeights, b.BoneWeights, t),
                BlendVertices = InterpolateBlend(a.BlendVertices, b.BlendVertices, t),
                BlendNormals = InterpolateBlend(a.BlendNormals, b.BlendNormals, t),
                BlendTangents = InterpolateBlend(a.BlendTangents, b.BlendTangents, t),
            };
            for (int i = 0; i < 8; i++) result.Uv[i] = Vector4.Lerp(a.Uv[i], b.Uv[i], t);
            return result;
        }

        private static Vector3[][] InterpolateBlend(Vector3[][] a, Vector3[][] b, float t)
        {
            if (a == null || b == null) return null;
            var result = new Vector3[a.Length][];
            for (int i = 0; i < a.Length; i++)
            {
                result[i] = new Vector3[a[i].Length];
                for (int j = 0; j < a[i].Length; j++) result[i][j] = Vector3.Lerp(a[i][j], b[i][j], t);
            }
            return result;
        }

        private static Dictionary<int, float> InterpolateWeight(Dictionary<int, float> a, Dictionary<int, float> b, float t)
        {
            var values = new Dictionary<int, float>();
            foreach (var influence in a) AddWeight(values, influence.Key, influence.Value * (1f - t));
            foreach (var influence in b) AddWeight(values, influence.Key, influence.Value * t);
            float total = values.Values.Sum();
            if (total > 0f)
                foreach (int key in values.Keys.ToArray()) values[key] /= total;
            var result = values;
            return result;
        }

        private static void AddWeight(Dictionary<int, float> values, int index, float weight)
        {
            if (weight <= 0f) return;
            values[index] = values.TryGetValue(index, out float current) ? current + weight : weight;
        }

        private static int MatchMajority(Color a, Color b, Color c, List<MeshColorFilterRule> rules)
        {
            int ra = Match(a, rules), rb = Match(b, rules), rc = Match(c, rules);
            if (ra == rb || ra == rc) return ra;
            if (rb == rc) return rb;
            int[] candidates = { ra, rb, rc };
            return candidates.Where(value => value >= 0).DefaultIfEmpty(-1).Min();
        }

        public static int Match(Color color, IReadOnlyList<MeshColorFilterRule> rules)
        {
            for (int i = 0; i < rules.Count; i++)
                if (rules[i] != null && Matches(color, rules[i])) return i;
            return -1;
        }

        public static bool Matches(Color color, MeshColorFilterRule rule)
        {
            if (rule == null || !rule.Enabled) return false;
            if (rule.Mode == MeshColorFilterMode.RgbDistance)
            {
                float dr = color.r - rule.TargetColor.r, dg = color.g - rule.TargetColor.g, db = color.b - rule.TargetColor.b;
                return Mathf.Sqrt(dr * dr + dg * dg + db * db) <= rule.RgbTolerance;
            }
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            hue *= 360f;
            Color.RGBToHSV(rule.HsvBaseColor, out float baseHue, out _, out _);
            float hueDifference = Mathf.Abs(hue - baseHue * 360f);
            hueDifference = Mathf.Min(hueDifference, 360f - hueDifference);
            bool hueMatch = hueDifference <= rule.HueTolerance;
            return hueMatch && InRange(saturation, rule.SaturationMin, rule.SaturationMax) && InRange(value, rule.ValueMin, rule.ValueMax);
        }

        private static bool InRange(float value, float min, float max) => value >= Mathf.Min(min, max) && value <= Mathf.Max(min, max);
        private static Color Average(Color a, Color b, Color c) => new Color((a.r + b.r + c.r) / 3f, (a.g + b.g + c.g) / 3f, (a.b + b.b + c.b) / 3f, (a.a + b.a + c.a) / 3f);

        private static void AddTriangle(OutputGroup group, Vertex a, Vertex b, Vertex c)
        {
            int start = group.Vertices.Count;
            group.Vertices.Add(a); group.Vertices.Add(b); group.Vertices.Add(c);
            group.Indices.Add(start); group.Indices.Add(start + 1); group.Indices.Add(start + 2);
        }

        private static string GetHierarchyPath(Transform target, Transform root)
        {
            var indices = new Stack<int>();
            Transform current = target;
            while (current != null && current != root)
            {
                indices.Push(current.GetSiblingIndex());
                current = current.parent;
            }
            if (current != root) return null;
            return target == root || indices.Count == 0 ? string.Empty : string.Join("/", indices);
        }

        private static string MakeSafeFileName(string value)
        {
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            char[] characters = value.ToCharArray();
            for (int i = 0; i < characters.Length; i++)
                if (Array.IndexOf(invalidCharacters, characters[i]) >= 0)
                    characters[i] = '_';
            return new string(characters);
        }

        private static Transform FindByPath(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path)) return root;
            Transform current = root;
            foreach (string part in path.Split('/'))
            {
                if (!int.TryParse(part, out int childIndex) || childIndex < 0 || childIndex >= current.childCount) return null;
                current = current.GetChild(childIndex);
            }
            return current;
        }
    }
}
