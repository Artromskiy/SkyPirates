Shader "Hidden/SlyPirates/HexTerrainStencilVolume"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            // Depth-clamp the far cap instead of clipping the closed volume at the camera far plane.
            ZClip Off
            Cull Off
            ColorMask 0
            Stencil
            {
                Ref 0
                Comp Always
                Pass Keep
                ZFailFront DecrWrap
                ZFailBack IncrWrap
            }
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:Setup
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct ShadowInstance { float4x4 objectToWorld; float4x4 worldToObject; };
            StructuredBuffer<ShadowInstance> _ShadowInstances;
            float4 _ShadowExtrusion;
            void Setup() {}
            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                uint instanceID : SV_InstanceID;
            };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings Vert(Attributes input)
            {
                ShadowInstance instance = _ShadowInstances[input.instanceID];
                float3 positionWS = mul(instance.objectToWorld, float4(input.positionOS, 1)).xyz;
                float3 normalWS = normalize(mul(input.normalOS, (float3x3)instance.worldToObject));
                float3 awayFromLight = normalize(_ShadowExtrusion.xyz);
                // Uniform displacement keeps adjacent faces welded and puts the near cap inside its caster.
                positionWS += awayFromLight * _ShadowExtrusion.w;
                positionWS += normalize(positionWS - _WorldSpaceCameraPos) * _ShadowExtrusion.w;
                if (dot(normalWS, awayFromLight) > 0)
                    positionWS += _ShadowExtrusion.xyz;
                Varyings output;
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
