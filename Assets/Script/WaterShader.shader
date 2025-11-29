Shader "Custom/URP_WaterWithFoam"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.3, 0.7, 0.9, 0.8)
        _DeepColor ("Deep Color", Color) = (0.1, 0.3, 0.6, 1.0)
        _DepthDistance ("Depth Distance", Float) = 1.0
        
        [Header(Foam Settings)]
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamDistance ("Foam Distance", Float) = 0.4
        _FoamNoiseScale ("Foam Noise Scale", Float) = 10.0
        _FoamNoiseSpeed ("Foam Noise Speed", Float) = 0.5
        _FoamIntensity ("Foam Intensity", Float) = 2.0
        
        [Header(Wave Settings)]
        _WaveSpeed ("Wave Speed", Float) = 0.5
        _WaveScale ("Wave Scale", Float) = 0.5
        _WaveHeight ("Wave Height", Float) = 0.1
        _WaveFrequency ("Wave Frequency", Float) = 1.0
        
        [Header(Surface)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.9
        _NormalScale ("Normal Scale", Float) = 0.5
        
        _MainTex ("Noise Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 100
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float _DepthDistance;
                
                float4 _FoamColor;
                float _FoamDistance;
                float _FoamNoiseScale;
                float _FoamNoiseSpeed;
                float _FoamIntensity;
                
                float _WaveSpeed;
                float _WaveScale;
                float _WaveHeight;
                float _WaveFrequency;
                
                float _Smoothness;
                float _NormalScale;
                
                float4 _MainTex_ST;
            CBUFFER_END
            
            // Simple noise function
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // Perlin-like noise
            float perlinNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Wave function
            float waves(float2 uv, float time)
            {
                float wave1 = sin(uv.x * _WaveFrequency + time * _WaveSpeed) * 0.5;
                float wave2 = sin(uv.y * _WaveFrequency * 0.7 + time * _WaveSpeed * 0.8) * 0.3;
                float wave3 = sin((uv.x + uv.y) * _WaveFrequency * 0.5 + time * _WaveSpeed * 1.2) * 0.2;
                
                return (wave1 + wave2 + wave3) * _WaveHeight;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // Apply waves
                float time = _Time.y;
                float waveOffset = waves(positionWS.xz * _WaveScale, time);
                positionWS.y += waveOffset;
                
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Screen space UV
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // Sample depth
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float surfaceDepth = input.positionCS.w;
                float depthDifference = sceneDepth - surfaceDepth;
                
                // Water color based on depth
                float depthFactor = saturate(depthDifference / _DepthDistance);
                float4 waterColor = lerp(_ShallowColor, _DeepColor, depthFactor);
                
                // Foam calculation
                float foamDepth = saturate(depthDifference / _FoamDistance);
                
                // Animated foam noise
                float time = _Time.y * _FoamNoiseSpeed;
                float2 foamUV1 = input.positionWS.xz * _FoamNoiseScale + float2(time, time * 0.5);
                float2 foamUV2 = input.positionWS.xz * _FoamNoiseScale * 0.7 + float2(-time * 0.8, time * 0.6);
                
                float foamNoise1 = perlinNoise(foamUV1);
                float foamNoise2 = perlinNoise(foamUV2);
                float foamNoise = (foamNoise1 + foamNoise2) * 0.5;
                
                // Create foam edge
                float foamEdge = 1.0 - foamDepth;
                foamEdge = pow(foamEdge, 3.0);
                
                // Combine foam with noise
                float foam = foamEdge * foamNoise * _FoamIntensity;
                foam = saturate(foam);
                
                // Mix water color with foam
                float4 finalColor = lerp(waterColor, _FoamColor, foam);
                
                // Simple lighting
                float3 lightDir = normalize(_MainLightPosition.xyz);
                float3 normalWS = normalize(input.normalWS);
                float NdotL = saturate(dot(normalWS, lightDir));
                
                finalColor.rgb *= _MainLightColor.rgb * (NdotL * 0.5 + 0.5);
                
                // Apply fog
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

