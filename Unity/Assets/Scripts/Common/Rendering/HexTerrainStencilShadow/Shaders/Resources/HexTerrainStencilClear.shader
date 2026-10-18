Shader "Hidden/SlyPirates/HexTerrainStencilClear"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Resolve Object Shadows"
            ZWrite Off
            ZTest Always
            Cull Off
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4x4 _ShadowInverseViewProjection;
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                output.uv = GetFullScreenTriangleTexCoord(vertexID);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float depth = LoadSceneDepth(input.positionCS.xy);
                #if !UNITY_REVERSED_Z
                    depth = depth * 2.0 - 1.0;
                #endif
                float3 positionWS = ComputeWorldSpacePosition(input.uv, depth, _ShadowInverseViewProjection);
                float4 shadowCoord = TransformWorldToShadowCoord(positionWS, false);
                return SampleMainLightRealtimeShadow(shadowCoord, false);
            }
            ENDHLSL
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            ColorMask 0
            Stencil
            {
                Ref 0
                ReadMask 255
                WriteMask 255
                Comp Always
                Pass Zero
            }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


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
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
