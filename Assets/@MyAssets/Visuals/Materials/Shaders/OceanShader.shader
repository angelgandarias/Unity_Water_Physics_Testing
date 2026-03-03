Shader "Custom/URP_OceanShader"
{
    Properties
    {
        [Header(Colors and Textures)]
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Deep Water Color", Color) = (0.0, 0.2, 0.5, 1)
        _TipColor ("Wave Tip Color", Color) = (0.0, 0.6, 0.8, 1)
        _ColorBlend ("Color Blend Spread", Range(0.1, 5.0)) = 1.5
        
        [Header(Surface Properties)]
        _Smoothness ("Smoothness", Range(0,1)) = 0.9
        _Metallic ("Metallic", Range(0,1)) = 0.0

        [Header(Gerstner Waves)]
        // (Dir X, Dir Z, Steepness, Wavelength)
        _WaveA ("Wave A", Vector) = (1, 0, 0.5, 10)
        _WaveB ("Wave B", Vector) = (0, 1, 0.25, 20)
        _WaveC ("Wave C", Vector) = (1, 1, 0.15, 10)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Required for URP Shadows to work properly
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD1;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : NORMAL;
                float waveHeight    : TEXCOORD2; // Used to pass displacement to fragment
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // SRP Batcher compatibility requires all properties in this block
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _TipColor;
                float _ColorBlend;
                float _Smoothness;
                float _Metallic;
                float4 _WaveA;
                float4 _WaveB;
                float4 _WaveC;
            CBUFFER_END

            float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal) 
            {
                float3 gwOutput;
                float steepness = wave.z;
                float wavelength = wave.w;
                float k = TWO_PI / wavelength;
                float c = sqrt(9.8 / k);
                float2 d = normalize(wave.xy);
                float f = k * (dot(d, p.xz) - c * _Time.y);
                float a = steepness / k;
                
                tangent += float3(
                    -d.x * d.x * (steepness * sin(f)),
                    d.x * (steepness * cos(f)),
                    -d.x * d.y * (steepness * sin(f))
                );
                binormal += float3(
                    -d.x * d.y * (steepness * sin(f)),
                    d.y * (steepness * cos(f)),
                    -d.y * d.y * (steepness * sin(f))
                );
                gwOutput = float3(
                    d.x * (a * cos(f)),
                    a * sin(f),
                    d.y * (a * cos(f))
                );
                return gwOutput;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // 1. Get world space position of the vertex
                float3 gridPoint = TransformObjectToWorld(input.positionOS.xyz);
                float3 tangent = float3(1, 0, 0);
                float3 binormal = float3(0, 0, 1);
                float3 p = gridPoint;
                
                // 2. Accumulate waves
                p += GerstnerWave(_WaveA, gridPoint, tangent, binormal);
                p += GerstnerWave(_WaveB, gridPoint, tangent, binormal);
                p += GerstnerWave(_WaveC, gridPoint, tangent, binormal);
                
                // 3. Calculate normal in World Space
                float3 calculatedNormal = normalize(cross(binormal, tangent));

                // 4. Output to Varyings
                output.positionCS = TransformWorldToHClip(p);
                output.positionWS = p;
                
                // FIX: Normal is already in World Space, so we just pass it directly!
                output.normalWS = calculatedNormal;
                output.uv = input.uv;
                
                // Store the Y displacement amount to color the wave peaks
                output.waveHeight = p.y - gridPoint.y; 

                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // 1. Calculate height-based color (Deep water vs Wave Peaks)
                float blendFactor = saturate((input.waveHeight + _ColorBlend * 0.5) / _ColorBlend);
                half3 finalColor = lerp(_BaseColor.rgb, _TipColor.rgb, blendFactor);

                // 2. Setup the surface data
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * finalColor;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.alpha = _BaseColor.a;

                // 3. Setup the input data for URP lighting
                InputData inputData = (InputData)0;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

                // 4. Calculate PBR Lighting
                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }
    }
}