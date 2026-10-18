using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Common.Rendering.HexTerrainStencilShadow
{
    internal sealed class HexTerrainStencilShadowFeature : ScriptableRendererFeature
    {
        private Material _clear;
        private Material _volume;
        private Material _overlay;
        private HexTerrainStencilShadowPass _pass;
        private readonly HexTerrainShadowKeywordsPass _restoreKeywords = new();

        public override void Create()
        {
            Release();
            _clear = LoadMaterial("HexTerrainStencilClear");
            _volume = LoadMaterial("HexTerrainStencilVolume");
            _overlay = LoadMaterial("HexTerrainStencilOverlay");
            _volume.enableInstancing = true;
            _pass = new HexTerrainStencilShadowPass(_clear, _volume, _overlay);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData;
            if (_pass != null && (camera.cameraType == CameraType.SceneView ||
                camera.cameraType == CameraType.Game && camera.renderType == CameraRenderType.Base))
            {
                renderer.EnqueuePass(_pass);
                renderer.EnqueuePass(_restoreKeywords);
            }
        }

        protected override void Dispose(bool disposing) => Release();

        private void Release()
        {
            _pass?.Dispose();
            _pass = null;
            CoreUtils.Destroy(_clear);
            CoreUtils.Destroy(_volume);
            CoreUtils.Destroy(_overlay);
        }

        private static Material LoadMaterial(string name)
        {
            Shader shader = Resources.Load<Shader>(name);
            if (shader == null) throw new InvalidOperationException($"Missing stencil shadow resource: {name}");
            return CoreUtils.CreateEngineMaterial(shader);
        }
    }

    internal sealed class HexTerrainStencilShadowPass : ScriptableRenderPass, IDisposable
    {
        internal static readonly GlobalKeyword MainShadows = GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS");
        internal static readonly GlobalKeyword CascadeShadows = GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS_CASCADE");
        internal static readonly GlobalKeyword ScreenShadows = GlobalKeyword.Create("_MAIN_LIGHT_SHADOWS_SCREEN");
        private static readonly int AttenuationId = Shader.PropertyToID("_TerrainShadowAttenuation");
        private static readonly int InverseViewProjectionId = Shader.PropertyToID("_ShadowInverseViewProjection");
        private static readonly int ScreenShadowsId = Shader.PropertyToID("_ScreenSpaceShadowmapTexture");
        private static readonly int InstancesId = Shader.PropertyToID("_ShadowInstances");
        private static readonly int ExtrusionId = Shader.PropertyToID("_ShadowExtrusion");
        private readonly Material _clear;
        private readonly Material _volume;
        private readonly Material _overlay;
        private readonly Dictionary<Mesh, Batch> _batches = new();
        private readonly List<Mesh> _removed = new();
        private readonly Plane[] _planes = new Plane[6];
        private int _version = -1;

        public HexTerrainStencilShadowPass(Material clear, Material volume, Material overlay)
        {
            _clear = clear;
            _volume = volume;
            _overlay = overlay;
            // Resolve object and terrain occlusion before opaque lighting samples it.
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses + 2;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            UpdateBatches();
            if (_batches.Count == 0) return;
            var resources = frameData.Get<UniversalResourceData>();
            var camera = frameData.Get<UniversalCameraData>().camera;
            var lights = frameData.Get<UniversalLightData>();
            if (!resources.cameraDepthTexture.IsValid() || lights.mainLightIndex < 0 ||
                !frameData.Get<UniversalShadowData>().supportsMainLightShadows) return;
            VisibleLight light = lights.visibleLights[lights.mainLightIndex];
            if (light.lightType != LightType.Directional || light.finalColor.maxColorComponent <= 0f) return;

            Vector3 direction = ((Vector3)light.localToWorldMatrix.GetColumn(2)).normalized;
            GeometryUtility.CalculateFrustumPlanes(camera, _planes);

            var descriptor = graph.GetTextureDesc(resources.cameraDepthTexture);
            descriptor.name = "Terrain and Object Screen Space Shadows";
            descriptor.format = GraphicsFormat.R8_UNorm;
            descriptor.msaaSamples = MSAASamples.None;
            descriptor.clearBuffer = false;
            TextureHandle shadows = graph.CreateTexture(descriptor);

            using (var resolve = graph.AddRasterRenderPass<ResolveData>("Resolve Object Shadows", out var resolveData))
            {
                resolveData.Material = _clear;
                resolveData.Cascades = frameData.Get<UniversalShadowData>().mainLightShadowCascadesCount > 1;
                resolveData.Depth = resources.cameraDepthTexture;
                resolveData.Camera = frameData.Get<UniversalCameraData>();
                resolve.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                resolve.UseTexture(resources.mainShadowsTexture, AccessFlags.Read);
                resolve.SetRenderAttachment(shadows, 0, AccessFlags.Write);
                resolve.AllowGlobalStateModification(true);
                resolve.SetRenderFunc(static (ResolveData pass, RasterGraphContext context) =>
                {
                    bool flipped = context.GetTextureUVOrigin(pass.Depth) == TextureUVOrigin.BottomLeft;
                    Matrix4x4 projection = GL.GetGPUProjectionMatrix(pass.Camera.GetProjectionMatrix(), flipped);
                    Matrix4x4 inverse = (projection * pass.Camera.GetViewMatrix()).inverse;
                    pass.Material.SetMatrix(InverseViewProjectionId, inverse);
                    context.cmd.SetKeyword(MainShadows, !pass.Cascades);
                    context.cmd.SetKeyword(CascadeShadows, pass.Cascades);
                    context.cmd.SetKeyword(ScreenShadows, false);
                    context.cmd.DrawProcedural(Matrix4x4.identity, pass.Material, 0, MeshTopology.Triangles, 3);
                });
            }

            using var builder = graph.AddRasterRenderPass<PassData>("Hex Terrain Stencil Shadows", out var data);
            data.Clear = _clear;
            data.Volume = _volume;
            data.Overlay = _overlay;
            data.Attenuation = 1f - light.light.shadowStrength;
            data.Batches.Clear();
            foreach (Batch batch in _batches.Values)
            {
                float distance = camera.farClipPlane * 2f +
                    Vector3.Distance(camera.transform.position, batch.Bounds.center) + batch.Bounds.extents.magnitude;
                Vector3 extrusion = direction * distance;
                Bounds swept = batch.Bounds;
                swept.Encapsulate(new Bounds(batch.Bounds.center + extrusion, batch.Bounds.size));
                swept.Expand(0.1f);
                if (!GeometryUtility.TestPlanesAABB(_planes, swept)) continue;
                batch.Properties.SetVector(ExtrusionId, new Vector4(extrusion.x, extrusion.y, extrusion.z, 0.005f));
                builder.UseBuffer(graph.ImportBuffer(batch.Buffer), AccessFlags.Read);
                data.Batches.Add(batch);
            }
            // Add terrain occlusion to the ordinary caster mask. No camera color overlay.
            builder.SetRenderAttachment(shadows, 0, AccessFlags.ReadWrite);
            builder.SetRenderAttachmentDepth(resources.cameraDepthTexture, AccessFlags.ReadWrite);
            builder.SetGlobalTextureAfterPass(shadows, ScreenShadowsId);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) =>
            {
                // Clear only stencil; preserve the object shadow attenuation.
                context.cmd.DrawProcedural(Matrix4x4.identity, pass.Clear, 1, MeshTopology.Triangles, 3);
                foreach (Batch batch in pass.Batches)
                    context.cmd.DrawMeshInstancedProcedural(batch.Mesh, 0, pass.Volume, 0, batch.Instances.Count, batch.Properties);
                context.cmd.SetGlobalFloat(AttenuationId, pass.Attenuation);
                context.cmd.DrawProcedural(Matrix4x4.identity, pass.Overlay, 0, MeshTopology.Triangles, 3);
                context.cmd.SetKeyword(MainShadows, false);
                context.cmd.SetKeyword(CascadeShadows, false);
                context.cmd.SetKeyword(ScreenShadows, true);
            });
        }

        private void UpdateBatches()
        {
            if (_version == HexTerrainStencilShadowRegistry.Version) return;
            _version = HexTerrainStencilShadowRegistry.Version;
            foreach (Batch batch in _batches.Values) batch.Instances.Clear();
            foreach (var entry in HexTerrainStencilShadowRegistry.Values)
            {
                if (entry.Mesh == null) continue;
                if (!_batches.TryGetValue(entry.Mesh, out Batch batch))
                {
                    batch = new Batch(entry.Mesh);
                    _batches.Add(entry.Mesh, batch);
                }
                if (batch.Instances.Count == 0) batch.Bounds = entry.Bounds;
                else batch.Bounds.Encapsulate(entry.Bounds);
                batch.Instances.AddRange(entry.Instances);
            }
            _removed.Clear();
            foreach (var pair in _batches)
            {
                if (pair.Value.Instances.Count == 0)
                {
                    pair.Value.Dispose();
                    _removed.Add(pair.Key);
                }
                else pair.Value.Upload();
            }
            foreach (Mesh mesh in _removed) _batches.Remove(mesh);
        }

        public void Dispose()
        {
            foreach (Batch batch in _batches.Values) batch.Dispose();
            _batches.Clear();
        }

        private sealed class ResolveData
        {
            internal Material Material;
            internal TextureHandle Depth;
            internal UniversalCameraData Camera;
            internal bool Cascades;
        }

        private sealed class PassData
        {
            internal Material Clear;
            internal Material Volume;
            internal Material Overlay;
            internal float Attenuation;
            internal readonly List<Batch> Batches = new();
        }

        private sealed class Batch : IDisposable
        {
            internal readonly Mesh Mesh;
            internal readonly List<HexTerrainStencilShadowRegistry.Instance> Instances = new();
            internal readonly MaterialPropertyBlock Properties = new();
            internal GraphicsBuffer Buffer;
            internal Bounds Bounds;

            internal Batch(Mesh mesh) => Mesh = mesh;

            internal void Upload()
            {
                int count = Instances.Count;
                if (Buffer == null || Buffer.count < count)
                {
                    Buffer?.Dispose();
                    Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Mathf.NextPowerOfTwo(count), 128);
                    Properties.SetBuffer(InstancesId, Buffer);
                }
                Buffer.SetData(Instances);
            }

            public void Dispose() => Buffer?.Dispose();

        }
    }

    // Transparent lighting uses the original shadow map, as in URP's screen-space feature.
    internal sealed class HexTerrainShadowKeywordsPass : ScriptableRenderPass
    {
        internal HexTerrainShadowKeywordsPass() => renderPassEvent = RenderPassEvent.BeforeRenderingTransparents + 1;

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var shadows = frameData.Get<UniversalShadowData>();
            using var builder = graph.AddRasterRenderPass<PassData>("Restore Shadow Keywords", out var data);
            data.Shadows = shadows.supportsMainLightShadows;
            data.Cascades = shadows.mainLightShadowCascadesCount > 1;
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) =>
            {
                context.cmd.SetKeyword(HexTerrainStencilShadowPass.ScreenShadows, false);
                context.cmd.SetKeyword(HexTerrainStencilShadowPass.MainShadows, pass.Shadows && !pass.Cascades);
                context.cmd.SetKeyword(HexTerrainStencilShadowPass.CascadeShadows, pass.Shadows && pass.Cascades);
            });
        }

        private sealed class PassData
        {
            internal bool Shadows;
            internal bool Cascades;
        }
    }

}
