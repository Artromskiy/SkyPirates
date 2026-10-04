using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace DVG.SkyPirates.Rendering.SphereOverlay.Internals
{
    internal sealed class SphereOverlayFeature : ScriptableRendererFeature
    {
        private static readonly int VolumeCountId = Shader.PropertyToID("_SphereOverlayVolumeCount");
        private static readonly int CentersId = Shader.PropertyToID("_SphereOverlayCenters");
        private static readonly int ColorsId = Shader.PropertyToID("_SphereOverlayColors");

        [SerializeField] private Shader shader;

        private Material _material;
        private SphereOverlayPass _pass;

        public override void Create()
        {
            if (_material != null)
                CoreUtils.Destroy(_material);

            if (shader == null)
                shader = Resources.Load<Shader>("SphereOverlay");

            if (shader != null)
            {
                _material = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _pass = new SphereOverlayPass(_material);
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null)
                return;

            var cameraData = renderingData.cameraData;
            var isGameCamera = cameraData.cameraType == CameraType.Game;
            var isSceneViewCamera = cameraData.cameraType == CameraType.SceneView;
            if (!isGameCamera && !isSceneViewCamera)
                return;

            if (isGameCamera && cameraData.renderType != CameraRenderType.Base)
                return;

            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            if (_material != null)
                CoreUtils.Destroy(_material);
            _material = null;
            _pass = null;
        }

        private sealed class SphereOverlayPass : ScriptableRenderPass
        {
            private const string PassName = "Sphere Overlay";
            private readonly Material _material;
            private readonly Vector4[] _centers = new Vector4[SphereOverlayRegistry.MaximumVolumes];
            private readonly Vector4[] _colors = new Vector4[SphereOverlayRegistry.MaximumVolumes];
            private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();

            public SphereOverlayPass(Material material)
            {
                _material = material;
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var volumeCount = SphereOverlayRegistry.CopyTo(_centers, _colors);
                if (volumeCount == 0)
                    return;

                var resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer ||
                    !resourceData.activeColorTexture.IsValid() ||
                    !resourceData.cameraDepthTexture.IsValid())
                    return;

                _properties.Clear();
                _properties.SetInt(VolumeCountId, volumeCount);
                _properties.SetVectorArray(CentersId, _centers);
                _properties.SetVectorArray(ColorsId, _colors);

                var source = resourceData.activeColorTexture;
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
                    builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);

                resourceData.cameraColor = destination;
            }
        }
    }
}
