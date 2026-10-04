using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace DVG.SkyPirates.Rendering.SphereOverlay.Internals
{
    internal sealed class SphereOverlayFeature : ScriptableRendererFeature
    {
        private Material _material;
        private Material _maskMaterial;
        private SphereOverlayPass _pass;

        public override void Create()
        {
            try
            {
                _pass?.Dispose();
                _pass = null;
                DisposeMaterials();

                var shader = Resources.Load<Shader>("SphereOverlay");
                var maskShader = Resources.Load<Shader>("SphereOverlayMask");
                if (shader == null || maskShader == null)
                {
                    UnityEngine.Debug.LogError(
                        "SphereOverlay could not load its shaders from Shaders/Resources. The feature is disabled.");
                    return;
                }

                _material = CoreUtils.CreateEngineMaterial(shader);
                _maskMaterial = CoreUtils.CreateEngineMaterial(maskShader);
                _pass = new SphereOverlayPass(
                    _material,
                    _maskMaterial,
                    Shader.PropertyToID("_SphereOverlayVolumeCount"),
                    Shader.PropertyToID("_SphereOverlayCenters"),
                    Shader.PropertyToID("_SphereOverlayGradientLut"),
                    Shader.PropertyToID("_SphereOverlayReceiverMask"),
                    new[]
                    {
                        new ShaderTagId("UniversalForward"),
                        new ShaderTagId("UniversalForwardOnly"),
                        new ShaderTagId("SRPDefaultUnlit")
                    });
            }
            catch (System.Exception exception)
            {
                _pass?.Dispose();
                _pass = null;
                DisposeMaterials();
                UnityEngine.Debug.LogError("SphereOverlay failed during renderer-feature initialization:\n" + exception);
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null)
                return;

            var cameraData = renderingData.cameraData;
            var isGameCamera = cameraData.cameraType == CameraType.Game;
            var isSceneViewCamera = cameraData.cameraType == CameraType.SceneView;
            if ((!isGameCamera && !isSceneViewCamera) ||
                (isGameCamera && cameraData.renderType != CameraRenderType.Base))
                return;

            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
            _pass = null;
            DisposeMaterials();
        }

        private void DisposeMaterials()
        {
            if (_material != null)
                CoreUtils.Destroy(_material);
            if (_maskMaterial != null)
                CoreUtils.Destroy(_maskMaterial);
            _material = null;
            _maskMaterial = null;
        }

        private sealed class SphereOverlayPass : ScriptableRenderPass
        {
            private const string PassName = "Sphere Overlay";
            private readonly Material _material;
            private readonly Material _maskMaterial;
            private readonly int _volumeCountId;
            private readonly int _centersId;
            private readonly int _gradientLutId;
            private readonly int _receiverMaskId;
            private readonly ShaderTagId[] _shaderTagIds;
            private readonly Vector4[] _centers = new Vector4[SphereOverlayRegistry.MaximumVolumes];
            private readonly Color32[] _gradientPixels = new Color32[
                SphereOverlayRegistry.MaximumVolumes * SphereOverlayRegistry.GradientResolution];
            private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();
            private readonly Texture2D _gradientLut;

            public SphereOverlayPass(
                Material material,
                Material maskMaterial,
                int volumeCountId,
                int centersId,
                int gradientLutId,
                int receiverMaskId,
                ShaderTagId[] shaderTagIds)
            {
                _material = material;
                _maskMaterial = maskMaterial;
                _volumeCountId = volumeCountId;
                _centersId = centersId;
                _gradientLutId = gradientLutId;
                _receiverMaskId = receiverMaskId;
                _shaderTagIds = shaderTagIds;
                _gradientLut = new Texture2D(
                    SphereOverlayRegistry.GradientResolution,
                    SphereOverlayRegistry.MaximumVolumes,
                    TextureFormat.RGBA32,
                    mipChain: false,
                    linear: true)
                {
                    name = "Sphere Overlay Gradient LUT",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public void Dispose() => CoreUtils.Destroy(_gradientLut);

            private sealed class MaskPassData
            {
                internal RendererListHandle RendererList;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var volumeCount = SphereOverlayRegistry.CopyTo(_centers, _gradientPixels);
                if (volumeCount == 0)
                    return;

                var resourceData = frameData.Get<UniversalResourceData>();
                var renderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var lightData = frameData.Get<UniversalLightData>();
                if (resourceData.isActiveTargetBackBuffer ||
                    !resourceData.activeColorTexture.IsValid() ||
                    !resourceData.activeDepthTexture.IsValid())
                    return;

                _gradientLut.SetPixels32(_gradientPixels);
                _gradientLut.Apply(updateMipmaps: false, makeNoLongerReadable: false);

                var source = resourceData.activeColorTexture;
                var descriptor = source.GetDescriptor(renderGraph);
                descriptor.name = "Sphere Overlay Receiver Mask";
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = MSAASamples.None;
                descriptor.colorFormat = GraphicsFormat.R8_UNorm;
                descriptor.clearBuffer = true;
                descriptor.clearColor = Color.clear;
                var receiverMask = renderGraph.CreateTexture(descriptor);

                var drawingSettings = RenderingUtils.CreateDrawingSettings(
                    _shaderTagIds[0], renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
                for (var i = 1; i < _shaderTagIds.Length; i++)
                    drawingSettings.SetShaderPassName(i, _shaderTagIds[i]);
                drawingSettings.overrideMaterial = _maskMaterial;
                drawingSettings.overrideMaterialPassIndex = 0;
                drawingSettings.perObjectData = PerObjectData.None;
                drawingSettings.enableInstancing = true;

                var filteringSettings = new FilteringSettings(
                    RenderQueueRange.opaque,
                    layerMask: ~0,
                    renderingLayerMask: SphereOverlayRenderConstants.ReceiverRenderLayerMask);
                var rendererList = renderGraph.CreateRendererList(new RendererListParams(
                    renderingData.cullResults,
                    drawingSettings,
                    filteringSettings));

                using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>(
                           "Sphere Overlay Receiver Mask", out var passData))
                {
                    passData.RendererList = rendererList;
                    builder.UseRendererList(rendererList);
                    builder.SetRenderAttachment(receiverMask, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                    builder.SetGlobalTextureAfterPass(receiverMask, _receiverMaskId);
                    builder.SetRenderFunc(static (MaskPassData data, RasterGraphContext context) =>
                    {
                        context.cmd.DrawRendererList(data.RendererList);
                    });
                }

                _properties.Clear();
                _properties.SetInt(_volumeCountId, volumeCount);
                _properties.SetVectorArray(_centersId, _centers);
                _properties.SetTexture(_gradientLutId, _gradientLut);

                var destinationDescriptor = source.GetDescriptor(renderGraph);
                destinationDescriptor.name = PassName;
                destinationDescriptor.depthBufferBits = 0;
                destinationDescriptor.msaaSamples = MSAASamples.None;
                var destination = renderGraph.CreateTexture(destinationDescriptor);
                var parameters = new RenderGraphUtils.BlitMaterialParameters(
                    source,
                    destination,
                    Vector2.one,
                    Vector2.zero,
                    _material,
                    0,
                    _properties,
                    0,
                    0,
                    geometry: RenderGraphUtils.FullScreenGeometryType.ProceduralTriangle);

                using (var builder = renderGraph.AddBlitPass(parameters, PassName, returnBuilder: true))
                {
                    builder.UseTexture(resourceData.activeDepthTexture, AccessFlags.Read);
                    builder.UseGlobalTexture(_receiverMaskId, AccessFlags.Read);
                }

                resourceData.cameraColor = destination;
            }
        }
    }
}
