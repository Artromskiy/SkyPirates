Shader "Hidden/SlyPirates/HexTerrainStencilOverlay"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off
            ColorMask R
            Stencil
            {
                Ref 0
                ReadMask 255
                WriteMask 255
                Comp NotEqual
                // Terrain uses the same light strength as ordinary casters. The union of occlusion
                // prevents object silhouettes from showing through terrain shadows.
                Pass Zero
                Fail Zero
            }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            half _TerrainShadowAttenuation;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                float2 position = float2((vertexID << 1) & 2, vertexID & 2);
                output.positionCS = float4(position * 2.0 - 1.0, 0.0, 1.0);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _TerrainShadowAttenuation;
            }
            ENDHLSL
        }
    }
}
