using NUnit.Framework;
using UnityEngine;

namespace DVG.SkyPirates.Tooling.MeshColorFilter.Editor.Tests
{
    public sealed class MeshColorFilterProcessorTests
    {
        [Test]
        public void RgbDistanceUsesEuclideanRgbAndExcludesAlpha()
        {
            var rule = new MeshColorFilterRule { Mode = MeshColorFilterMode.RgbDistance, TargetColor = new Color(0.5f, 0.5f, 0.5f, 0f), RgbTolerance = 0.1f };
            Assert.IsTrue(MeshColorFilterProcessor.Matches(new Color(0.55f, 0.5f, 0.5f, 1f), rule));
            Assert.IsFalse(MeshColorFilterProcessor.Matches(new Color(0.8f, 0.5f, 0.5f), rule));
        }

        [Test]
        public void HsvBaseColorAndToleranceSupportHueWrapAndSaturationValueLimits()
        {
            var rule = new MeshColorFilterRule
            {
                Mode = MeshColorFilterMode.HsvRange,
                HsvBaseColor = Color.red,
                HueTolerance = 10f,
                SaturationMin = 0.8f,
                SaturationMax = 1f,
                ValueMin = 0.8f,
                ValueMax = 1f
            };
            Assert.IsTrue(MeshColorFilterProcessor.Matches(Color.red, rule));
            Assert.IsTrue(MeshColorFilterProcessor.Matches(Color.HSVToRGB(359f / 360f, 1f, 1f), rule));
            Assert.IsFalse(MeshColorFilterProcessor.Matches(Color.green, rule));
            Assert.IsFalse(MeshColorFilterProcessor.Matches(new Color(1f, 0.5f, 0.5f), rule));
        }

        [Test]
        public void FirstMatchingRuleHasPriority()
        {
            var first = new MeshColorFilterRule { Mode = MeshColorFilterMode.RgbDistance, TargetColor = Color.red, RgbTolerance = 1f };
            var second = new MeshColorFilterRule { Mode = MeshColorFilterMode.RgbDistance, TargetColor = Color.green, RgbTolerance = 1f };
            Assert.AreEqual(0, MeshColorFilterProcessor.Match(Color.yellow, new[] { first, second }));
        }

        [Test]
        public void AverageAssignmentPreservesUnmatchedTrianglesAndGroupsMatchedTriangles()
        {
            var mesh = new Mesh();
            mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up, Vector3.one, Vector3.right * 2f, Vector3.up * 2f };
            mesh.colors = new[] { Color.red, Color.red, Color.red, Color.blue, Color.blue, Color.blue };
            mesh.bindposes = new[] { Matrix4x4.identity };
            mesh.boneWeights = new[]
            {
                new BoneWeight { boneIndex0 = 0, weight0 = 1f }, new BoneWeight { boneIndex0 = 0, weight0 = 1f },
                new BoneWeight { boneIndex0 = 0, weight0 = 1f }, new BoneWeight { boneIndex0 = 0, weight0 = 1f },
                new BoneWeight { boneIndex0 = 0, weight0 = 1f }, new BoneWeight { boneIndex0 = 0, weight0 = 1f }
            };
            mesh.AddBlendShapeFrame("Test", 100f, new Vector3[6], new Vector3[6], new Vector3[6]);
            mesh.subMeshCount = 1;
            mesh.SetTriangles(new[] { 0, 1, 2, 3, 4, 5 }, 0);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var profile = ScriptableObject.CreateInstance<MeshColorFilterProfile>();
            profile.Rules.Add(new MeshColorFilterRule { ZoneName = "Red", Material = material, Mode = MeshColorFilterMode.RgbDistance, TargetColor = Color.red, RgbTolerance = 0.01f });

            try
            {
                Assert.IsTrue(MeshColorFilterProcessor.TryBuildMesh(mesh, new[] { material }, profile, out Mesh output, out Material[] materials, out string message), message);
                Assert.AreEqual(2, output.subMeshCount);
                Assert.AreEqual(1, output.GetTriangles(0).Length / 3);
                Assert.AreEqual(1, output.GetTriangles(1).Length / 3);
                Assert.AreSame(material, materials[0]);
                Assert.AreSame(material, materials[1]);
                Assert.AreEqual(1, output.bindposes.Length);
                Assert.AreEqual(1, output.blendShapeCount);
                var outputBoneCounts = output.GetBonesPerVertex();
                Assert.AreEqual(output.vertexCount, outputBoneCounts.Length);
                outputBoneCounts.Dispose();
                Object.DestroyImmediate(output);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void MajorityAndAdaptiveSubdivisionAssignBoundaryTriangles()
        {
            var mesh = new Mesh();
            mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            mesh.colors = new[] { Color.red, Color.red, Color.blue };
            mesh.triangles = new[] { 0, 1, 2 };
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var profile = ScriptableObject.CreateInstance<MeshColorFilterProfile>();
            profile.Rules.Add(new MeshColorFilterRule { ZoneName = "Red", Material = material, Mode = MeshColorFilterMode.RgbDistance, TargetColor = Color.red, RgbTolerance = 0.75f });
            try
            {
                profile.TriangleMode = MeshTriangleAssignmentMode.MajorityVertices;
                Assert.IsTrue(MeshColorFilterProcessor.TryBuildMesh(mesh, new[] { material }, profile, out Mesh majority, out _, out string majorityError), majorityError);
                Assert.AreEqual(1, majority.subMeshCount);
                Assert.AreEqual(1, majority.GetTriangles(0).Length / 3);
                Object.DestroyImmediate(majority);

                profile.TriangleMode = MeshTriangleAssignmentMode.AdaptiveSubdivision;
                profile.AdaptiveSplitDepth = 2;
                Assert.IsTrue(MeshColorFilterProcessor.TryBuildMesh(mesh, new[] { material }, profile, out Mesh split, out _, out string splitError), splitError);
                Assert.Greater(split.GetTriangles(0).Length / 3, 1);
                Object.DestroyImmediate(split);
                Assert.AreEqual(3, mesh.triangles.Length);
                Assert.AreEqual(Color.blue, mesh.colors[2]);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(profile);
            }
        }
    }
}
