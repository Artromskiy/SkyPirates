Shader "SkyPirates/UI/RoundRect"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UIRect"

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 shape : TEXCOORD0;
                float4 radii : TEXCOORD1;
                float4 style : TEXCOORD2;
                float4 effects : TEXCOORD3;
                float3 effectColor : NORMAL;
                float4 shadowColor : TANGENT;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 shape : TEXCOORD0;
                float4 radii : TEXCOORD1;
                float4 style : TEXCOORD2;
                float4 effects : TEXCOORD3;
                float3 effectColor : TEXCOORD4;
                float4 shadowColor : TEXCOORD5;
                float4 worldPosition : TEXCOORD6;
                float4 spriteUv : TEXCOORD7;
            };

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            float _EnableExternalAlpha;
            float4 _ClipRect;

            float4 SampleSpriteTexture(float2 uv)
            {
                float4 sprite = tex2D(_MainTex, uv);
                sprite.a = lerp(sprite.a, tex2D(_AlphaTex, uv).r, _EnableExternalAlpha);
                return sprite;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float spriteLayer = step(1.5, input.effects.w);
                float effectsOnly = step(input.effects.w, -0.5);
                float4 regularStyle = lerp(input.shadowColor, input.style, effectsOnly);

                output.vertex = UnityObjectToClipPos(input.vertex);
                output.worldPosition = input.vertex;
                output.color = input.color;
                output.spriteUv = input.shape * spriteLayer;
                output.shape = lerp(input.shape, input.radii, spriteLayer);
                output.radii = lerp(input.radii, input.style, spriteLayer);
                output.style = lerp(regularStyle, input.shadowColor, spriteLayer);
                output.effects = input.effects;
                output.effectColor = input.effectColor;
                output.shadowColor = input.shadowColor * effectsOnly * (1.0 - spriteLayer);

                return output;
            }

            float BoxDistance(float2 p, float2 halfSize)
            {
                float2 q = abs(p) - halfSize;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
            }

            void NormalizeRadii(float2 size, inout float4 radiusX, inout float4 radiusY)
            {
                radiusX = max(radiusX, 0.0);
                radiusY = max(radiusY, 0.0);

                float2 sumsX = radiusX.xw + radiusX.yz;
                float2 sumsY = radiusY.xy + radiusY.wz;
                float2 maxSums = float2(max(sumsX.x, sumsX.y), max(sumsY.x, sumsY.y));

                // sum == 0 => scale must be 1. No epsilon required.
                float2 hasSum = sign(maxSums);
                float2 safeSums = maxSums + (1.0 - hasSum);
                float2 scale = lerp(1.0, min(1.0, size / safeSums), hasSum);

                radiusX *= scale.x;
                radiusY *= scale.y;
            }

            float4 EllipseDistances(float4 dx, float4 dy, float4 radiusX, float4 radiusY)
            {
                // Zero-radius lanes are never selected as corners.
                // Replace their divisor by 1 purely as a neutral computational value.
                float4 validRadius = sign(min(radiusX, radiusY));
                radiusX += 1.0 - validRadius;
                radiusY += 1.0 - validRadius;

                float4 qx = dx / radiusX;
                float4 qy = dy / radiusY;
                float4 qLength = sqrt(qx * qx + qy * qy);

                // At ellipse center qLength == 0. Expected signed distance is
                // -min(radius), not a division by an arbitrary epsilon.
                float4 hasLength = sign(qLength);
                float4 safeLength = qLength + (1.0 - hasLength);
                float4 gx = qx / (radiusX * safeLength);
                float4 gy = qy / (radiusY * safeLength);
                float4 gradientLength = sqrt(gx * gx + gy * gy);
                float4 hasGradient = sign(gradientLength);
                float4 safeGradient = gradientLength + (1.0 - hasGradient);

                float4 distance = (qLength - 1.0) / safeGradient;
                distance = lerp(-min(radiusX, radiusY), distance, hasLength * hasGradient);

                return distance * validRadius;
            }

            // Corner order: TL, TR, BR, BL.
            float RoundedRectDistance(float2 p, float2 minPos, float2 maxPos, float4 radiusX, float4 radiusY)
            {
                float2 size = maxPos - minPos;
                NormalizeRadii(size, radiusX, radiusY);

                float2 center = (minPos + maxPos) * 0.5;
                float boxDistance = BoxDistance(p - center, size * 0.5);

                float4 centerX = float4(minPos.x, maxPos.x, maxPos.x, minPos.x) + radiusX * float4(1.0, -1.0, -1.0, 1.0);
                float4 centerY = float4(maxPos.y, maxPos.y, minPos.y, minPos.y) + radiusY * float4(-1.0, -1.0, 1.0, 1.0);
                float4 ellipseDistance = EllipseDistances(p.x - centerX, p.y - centerY, radiusX, radiusY);

                float4 edgeX = float4(p.x - minPos.x, maxPos.x - p.x, maxPos.x - p.x, p.x - minPos.x);
                float4 edgeY = float4(maxPos.y - p.y, maxPos.y - p.y, p.y - minPos.y, p.y - minPos.y);
                float4 mask = step(edgeX, radiusX) * step(edgeY, radiusY) * sign(min(radiusX, radiusY));

                float maskCount = dot(mask, float4(1.0, 1.0, 1.0, 1.0));
                float safeMaskCount = maskCount + (1.0 - sign(maskCount));
                float cornerDistance = dot(mask, ellipseDistance) / safeMaskCount;

                return lerp(boxDistance, cornerDistance, saturate(maskCount));
            }

            float InsetRoundedDistance(float4 radii, float4 borderWidths, float2 p, float2 halfSize)
            {
                float2 minPos = -halfSize + borderWidths.xy;
                float2 maxPos = halfSize - borderWidths.zw;
                float2 rawSize = maxPos - minPos;

                // Invalid/collapsed inner rect means "there is no fill region".
                float valid = saturate(sign(min(rawSize.x, rawSize.y)));
                float2 center = (minPos + maxPos) * 0.5;
                float2 safeSize = rawSize * valid + (1.0 - valid);

                minPos = center - safeSize * 0.5;
                maxPos = center + safeSize * 0.5;

                float4 radiusX = max(radii - borderWidths.xzzx, 0.0);
                float4 radiusY = max(radii - borderWidths.wwyy, 0.0);
                float distance = RoundedRectDistance(p, minPos, maxPos, radiusX, radiusY);
                float invalidDistance = max(halfSize.x, halfSize.y) + max(max(borderWidths.x, borderWidths.y), max(borderWidths.z, borderWidths.w));

                return lerp(invalidDistance, distance, valid);
            }

            float Edge(float distance) { return fwidth(distance) * 0.5; }

            float Coverage(float distance)
            {
                float edge = Edge(distance);
                float hasEdge = sign(edge);
                float safeEdge = edge + (1.0 - hasEdge);
                float smoothCoverage = 1.0 - smoothstep(-safeEdge, safeEdge, distance);
                float hardCoverage = step(distance, 0.0);
                return lerp(hardCoverage, smoothCoverage, hasEdge);
            }

            float4 Over(float4 foreground, float4 background) { return foreground + background * (1.0 - foreground.a); }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 localPosition = input.shape.xy;
                float2 halfSize = input.shape.zw;
                float2 minPos = -halfSize;
                float2 maxPos = halfSize;

                float spriteLayer = step(1.5, input.effects.w);
                float effectsOnly = step(input.effects.w, -0.5);

                float distance = RoundedRectDistance(localPosition, minPos, maxPos, input.radii, input.radii);
                float outerCoverage = Coverage(distance);

                // Effects
                float glowAlpha = (1.0 - smoothstep(0.0, input.effects.z + Edge(distance), max(distance, 0.0))) * saturate(input.style.x) * sign(input.effects.z);

                float shadowDistance = RoundedRectDistance(localPosition - input.effects.xy, minPos, maxPos, input.radii, input.radii) - input.style.w;
                float shadowWidth = input.style.z + fwidth(shadowDistance);
                float hasShadowWidth = sign(shadowWidth);
                float safeShadowWidth = shadowWidth + (1.0 - hasShadowWidth);
                float shadowAlpha = lerp(step(shadowDistance, 0.0), 1.0 - smoothstep(0.0, safeShadowWidth, max(shadowDistance, 0.0)), hasShadowWidth) * input.shadowColor.a;

                float4 shadow = float4(input.shadowColor.rgb * shadowAlpha, shadowAlpha);
                float4 glow = float4(input.effectColor * glowAlpha, glowAlpha);
                float4 effectsResult = Over(shadow, glow);

                // Fill / Sprite
                float2 fitPosition = localPosition - input.effects.xy;
                float2 insideFit2 = step(abs(fitPosition), input.spriteUv.zw);
                float insideFit = insideFit2.x * insideFit2.y;

                float4 sprite = SampleSpriteTexture(input.spriteUv.xy) * input.color;
                float grayscale = step(2.5, input.effects.w);
                float luminance = dot(sprite.rgb, float3(0.2126, 0.7152, 0.0722));
                sprite.rgb = lerp(sprite.rgb, luminance.xxx, grayscale);
                float fillAlpha = lerp(input.color.a, sprite.a * insideFit, spriteLayer);
                float3 fillRgb = lerp(input.color.rgb, sprite.rgb, spriteLayer);

                // Border
                float borderOpacity = saturate(input.effects.z);
                float borderWidth = max(max(input.style.x, input.style.y), max(input.style.z, input.style.w));
                float borderEnabled = sign(borderWidth);

                float4 fill = float4(fillRgb * fillAlpha, fillAlpha);
                float4 border = float4(input.effectColor * borderOpacity, borderOpacity);
                float4 borderedFill = Over(border, fill);

                float innerDistance = InsetRoundedDistance(input.radii, input.style, localPosition, halfSize);
                float innerCoverage = min(Coverage(innerDistance), outerCoverage);

                // Width == 0 has exact semantics:
                // no stroke whatsoever, fill is exactly the outer shape.
                float fillCoverage = lerp(outerCoverage, innerCoverage, borderEnabled);
                float strokeCoverage = Coverage(max(distance, -innerDistance)) * borderEnabled;

                float4 fillResult = fillCoverage * fill + strokeCoverage * borderedFill;
                float4 result = lerp(fillResult, effectsResult, effectsOnly);
                float clipFactor = 1.0;

                #ifdef UNITY_UI_CLIP_RECT
                clipFactor = UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                result *= clipFactor;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                float specialLayer = saturate(spriteLayer + effectsOnly);
                float alphaClip = lerp(result.a, outerCoverage * clipFactor, specialLayer);
                clip(alphaClip - 0.001);
                #endif

                return result;
            }

            ENDCG
        }
    }
}
