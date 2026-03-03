Shader "Custom/URPWireframeTransparent"
{
    Properties
    {
        [HDR] _WireColor("Wire Color", Color) = (0, 1, 0, 1)
        _WireThickness("Wire Thickness", Range(0, 1)) = 0.05
    }

    SubShader
    {
        // 1. Change Tags for Transparency
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }
        
        LOD 100

        Pass
        {
            Name "ForwardLit"
            
            // 2. Enable Alpha Blending
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off 

            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct v2g
            {
                float4 projection : SV_POSITION;
            };

            struct g2f
            {
                float4 projection : SV_POSITION;
                float3 barycentric : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _WireColor;
                float _WireThickness;
            CBUFFER_END

            v2g vert(Attributes v)
            {
                v2g o;
                o.projection = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                g2f o;
                float3 barys[3] = {
                    float3(1, 0, 0),
                    float3(0, 1, 0),
                    float3(0, 0, 1)
                };

                for (int i = 0; i < 3; i++)
                {
                    o.projection = input[i].projection;
                    o.barycentric = barys[i];
                    triStream.Append(o);
                }
            }

            half4 frag(g2f i) : SV_Target
            {
                // Calculate wire intensity
                float minBary = min(i.barycentric.x, min(i.barycentric.y, i.barycentric.z));
                float delta = fwidth(minBary);
                float wire = smoothstep(_WireThickness, _WireThickness - delta, minBary);

                // 3. Set Alpha based on the wire calculation
                float4 finalColor = _WireColor;
                finalColor.a *= wire; 

                // If the wire is 0, the pixel is fully transparent
                return finalColor;
            }
            ENDHLSL
        }
    }
}