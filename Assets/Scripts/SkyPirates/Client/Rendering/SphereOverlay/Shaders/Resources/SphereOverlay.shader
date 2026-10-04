Shader "Hidden/SkyPirates/SphereOverlay"
{
    Properties
    {
        _SphereOverlayVolumeCount ("Sphere Overlay Volume Count", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _SphereOverlayVolumeCount;
                float4 _SphereOverlayCenters[32];
                float4 _SphereOverlayCenterColors[32];
                float4 _SphereOverlayEdgeColors[32];
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 sceneColor = SAMPLE_TEXTURE2D_X_LOD(
                    _BlitTexture,
                    sampler_LinearClamp,
                    input.texcoord.xy,
                    _BlitMipLevel);

                float2 screenUv = input.texcoord.xy;
                float rawDepth = SampleSceneDepth(screenUv);
                float deviceDepth;
                #if UNITY_REVERSED_Z
                    if (rawDepth <= 0.000001)
                        return sceneColor;
                    deviceDepth = rawDepth;
                #else
                    if (rawDepth >= 0.999999)
                        return sceneColor;
                    deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
                #endif

                float3 worldPosition = ComputeWorldSpacePosition(screenUv, deviceDepth, UNITY_MATRIX_I_VP);
                float3 weightedColor = 0.0;
                float totalWeight = 0.0;
                float remainingAlpha = 1.0;

                [loop]
                for (int index = 0; index < (int)_SphereOverlayVolumeCount; index++)
                {
                    float4 centerRadius = _SphereOverlayCenters[index];
                    float3 delta = worldPosition - centerRadius.xyz;
                    float distanceSquared = dot(delta, delta);
                    float radiusSquared = centerRadius.w * centerRadius.w;
                    if (distanceSquared > radiusSquared)
                        continue;

                    float distanceFromCenter = sqrt(distanceSquared);
                    float gradientPosition = saturate(distanceFromCenter / max(centerRadius.w, 0.00001));
                    float4 volumeColor = lerp(
                        _SphereOverlayCenterColors[index],
                        _SphereOverlayEdgeColors[index],
                        gradientPosition);
                    float alpha = saturate(volumeColor.a);
                    weightedColor += volumeColor.rgb * alpha;
                    totalWeight += alpha;
                    remainingAlpha *= 1.0 - alpha;
                }

                if (totalWeight <= 0.0)
                    return sceneColor;

                float overlayAlpha = 1.0 - remainingAlpha;
                float3 overlayColor = weightedColor / totalWeight;
                sceneColor.rgb = lerp(sceneColor.rgb, overlayColor, overlayAlpha);
                return sceneColor;
            }
            ENDHLSL
        }
    }
}
