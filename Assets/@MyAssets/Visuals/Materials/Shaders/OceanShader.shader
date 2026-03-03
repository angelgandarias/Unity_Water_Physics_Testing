Shader "Custom/URP_OceanShader"
{
    Properties
    {
        [Header(Colors and Textures)]
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Deep Water Color", Color) = (0.0, 0.1, 0.2, 1)
        _TipColor ("Wave Tip Color", Color) = (0.0, 0.3, 0.5, 1)
        _ColorBlend ("Color Blend Spread", Range(0.1, 5.0)) = 1.5
        
        [Header(Interactive Wake)]
        [NoScaleOffset] _WakeMap ("Wake Render Texture", 2D) = "black" {}
        // X = Cam World X, Y = Cam World Z, Z = Cam Ortho Size
        _WakeParams ("Wake Camera Params (X, Z, Size)", Vector) = (0, 0, 50, 0)
        _WakeDepth ("Wake Flatten Strength", Range(0, 1)) = 0.8

        [Header(Foam and SSS)]
        _FoamColor ("Foam Color", Color) = (0.9, 0.9, 1.0, 1)
        _FoamThreshold ("Foam Height Threshold", Float) = 1.5
        _FoamSpread ("Foam Spread", Float) = 0.5
        _SSSColor ("Subsurface Scattering Color", Color) = (0.0, 0.8, 0.6, 1)
        _SSSStrength ("SSS Strength", Range(0, 5)) = 1.5

        [Header(Surface Ripples)]
        [Normal] _NormalMap ("Ripple Normal Map", 2D) = "bump" {}
        _NormalStrength ("Ripple Strength", Range(0, 2)) = 0.5
        _RippleScale ("Ripple Scale", Float) = 0.5
        _RippleSpeed ("Ripple Speeds (XY / ZW)", Vector) = (0.1, 0.05, -0.05, 0.1)

        [Header(Surface Properties)]
        _Smoothness ("Smoothness", Range(0,1)) = 0.9
        _Metallic ("Metallic", Range(0,1)) = 0.0

        [Header(Gerstner Waves Array)]
        _Wave1 ("Wave 1 (Deep Swell)", Vector) = (0.85, 0.25, 0.25, 31.0)
        _Wave2 ("Wave 2 (Deep Swell)", Vector) = (0.7, 0.5, 0.2, 23.0)
        _Wave3 ("Wave 3 (Cross Swell)", Vector) = (-0.15, 0.95, 0.15, 14.3)
        _Wave4 ("Wave 4 (Cross Swell)", Vector) = (0.3, 0.8, 0.15, 11.2)
        _Wave5 ("Wave 5 (Local Chop)", Vector) = (0.5, -0.6, 0.1, 7.1)
        _Wave6 ("Wave 6 (Local Chop)", Vector) = (-0.4, -0.3, 0.1, 4.3)
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
                float3 tangentWS    : TANGENT;
                float3 bitangentWS  : BITANGENT;
                float waveHeight    : TEXCOORD2;
                float wakeIntensity : TEXCOORD3; // Pass wake data to fragment
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            // We need a specific texture sampler macro for vertex shaders
            TEXTURE2D(_WakeMap);
            SAMPLER(sampler_WakeMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _TipColor;
                float _ColorBlend;
                
                float4 _WakeParams;
                float _WakeDepth;

                float4 _FoamColor;
                float _FoamThreshold;
                float _FoamSpread;
                float4 _SSSColor;
                float _SSSStrength;

                float _NormalStrength;
                float _RippleScale;
                float4 _RippleSpeed;
                float _Smoothness;
                float _Metallic;
                
                float4 _Wave1;
                float4 _Wave2;
                float4 _Wave3;
                float4 _Wave4;
                float4 _Wave5;
                float4 _Wave6;
            CBUFFER_END

            float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal) 
            {
                float3 gwOutput = float3(0,0,0);
                float steepness = wave.z;
                float wavelength = wave.w;
                
                if (wavelength <= 0.001) return gwOutput; 

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
                
                float3 gridPoint = TransformObjectToWorld(input.positionOS.xyz);
                float3 tangent = float3(1, 0, 0);
                float3 binormal = float3(0, 0, 1);
                float3 p = gridPoint;
                
                float4 waves[6] = { _Wave1, _Wave2, _Wave3, _Wave4, _Wave5, _Wave6 };
                
                for (int i = 0; i < 6; i++)
                {
                    p += GerstnerWave(waves[i], gridPoint, tangent, binormal);
                }

                // --- NEW WAKE LOGIC ---
                // 1. Calculate the UV coordinate based on world position and camera bounds
                float2 wakeUV = (gridPoint.xz - _WakeParams.xy) / (_WakeParams.z * 2.0) + 0.5;

                float wakeIntensity = 0;
                if (wakeUV.x >= 0 && wakeUV.x <= 1 && wakeUV.y >= 0 && wakeUV.y <= 1)
                {
                    wakeIntensity = SAMPLE_TEXTURE2D_LOD(_WakeMap, sampler_WakeMap, wakeUV, 0).r;
                }

                // Store the natural, unflattened wave position
                float3 naturalWavePos = p;

                // Flatten the water towards the base grid
                p = lerp(p, gridPoint, wakeIntensity * _WakeDepth);

                // THE FIX: Prevent upwards displacement!
                // If our new flattened Y is higher than the natural wave Y, force it to stay down.
                p.y = min(p.y, naturalWavePos.y); 

                output.wakeIntensity = wakeIntensity;
                // ----------------------
                
                float3 calculatedNormal = normalize(cross(binormal, tangent));

                output.positionCS = TransformWorldToHClip(p);
                output.positionWS = p;
                
                output.normalWS = calculatedNormal;
                output.tangentWS = normalize(tangent);
                output.bitangentWS = normalize(binormal);
                
                output.uv = input.uv;
                output.waveHeight = p.y - gridPoint.y; 

                return output;
            }
            
            half3 BlendNormals(half3 n1, half3 n2)
            {
                return normalize(half3(n1.xy + n2.xy, n1.z * n2.z));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float blendFactor = saturate((input.waveHeight + _ColorBlend * 0.5) / _ColorBlend);
                half3 baseWaveColor = lerp(_BaseColor.rgb, _TipColor.rgb, blendFactor);

                float2 baseUV = input.positionWS.xz * _RippleScale;
                float2 scroll1 = baseUV + (_Time.y * _RippleSpeed.xy);
                float2 scroll2 = (baseUV * 1.3) + (_Time.y * _RippleSpeed.zw); 

                half4 map1 = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, scroll1);
                half4 map2 = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, scroll2);
                half3 normal1 = UnpackNormalScale(map1, _NormalStrength);
                half3 normal2 = UnpackNormalScale(map2, _NormalStrength);

                half3 tangentNormal = BlendNormals(normal1, normal2);
                half3x3 tbn = half3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                half3 finalNormalWS = normalize(mul(tangentNormal, tbn));

                // Wave Peak Foam
                float foamNoise = (normal1.x + normal2.y) * 0.5; 
                float waveFoamFactor = smoothstep(_FoamThreshold - _FoamSpread, _FoamThreshold + _FoamSpread, input.waveHeight + foamNoise);
                
                // Combine Peak Foam with the new Wake Trail Foam!
                float totalFoamFactor = saturate(waveFoamFactor + input.wakeIntensity);

                half3 colorWithFoam = lerp(baseWaveColor, _FoamColor.rgb, totalFoamFactor);

                InputData inputData = (InputData)0;
                inputData.normalWS = finalNormalWS; 
                inputData.viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

                Light mainLight = GetMainLight(inputData.shadowCoord);
                
                half backlight = max(0, dot(inputData.viewDirectionWS, -mainLight.direction));
                half sssFactor = pow(backlight, 4.0) * saturate(input.waveHeight * 0.5) * _SSSStrength;
                
                half3 finalAlbedo = colorWithFoam + (_SSSColor.rgb * sssFactor * mainLight.color);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * finalAlbedo;
                surfaceData.metallic = _Metallic;
                // Keep rough wherever there is foam (peaks or wake trail)
                surfaceData.smoothness = lerp(_Smoothness, 0.0, totalFoamFactor); 
                surfaceData.alpha = _BaseColor.a;

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }
    }
}