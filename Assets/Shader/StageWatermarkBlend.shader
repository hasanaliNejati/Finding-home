Shader "Custom/StageWatermarkBlend"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _WatermarkTex ("Watermark Texture", 2D) = "black" {}
        _WatermarkStrength ("Watermark Strength", Range(0, 1)) = 1
        [Toggle] _UseWatermark ("Use Watermark", Float) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1; // UV2 for watermark
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_WatermarkTex);
            SAMPLER(sampler_WatermarkTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _WatermarkStrength;
                float _UseWatermark;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.uv2 = input.uv2;
                output.normalWS = normalInput.normalWS;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample base texture
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                // Sample watermark texture with UV2
                half4 watermarkColor = SAMPLE_TEXTURE2D(_WatermarkTex, sampler_WatermarkTex, input.uv2);
                
                // Blend watermark with base
                half4 finalColor = baseColor;
                if (_UseWatermark > 0.5)
                {
                    // Alpha blend: watermark over base
                    finalColor.rgb = lerp(baseColor.rgb, watermarkColor.rgb, watermarkColor.a * _WatermarkStrength);
                }
                
                // Apply fog
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}

