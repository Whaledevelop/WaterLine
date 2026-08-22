Shader "Game/AnimatedWater"
{
    Properties
    {
        _MainTex("Water Texture", 2D) = "white" {}
        _LayerOffset("Layer Offset", Vector) = (0,0,0,0)
        _DetailOffset("Detail Offset", Vector) = (0,0,0,0)
        _DetailStrength("Detail Strength", Range(0,1)) = 0.35
        _LayerScale("Layer Scale", Float) = 0.22
        _DetailScale("Detail Scale", Float) = 0.41
        _HighlightContrast("Highlight Contrast", Range(0.25,4)) = 1.35
        _Tint("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _LayerOffset;
            float4 _DetailOffset;
            float4 _Tint;
            float _DetailStrength;
            float _LayerScale;
            float _DetailScale;
            float _HighlightContrast;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * _MainTex_ST.xy;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 worldUv = input.positionWS.xy;
                half4 layer = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, worldUv * _LayerScale + _LayerOffset.xy);
                half4 detail = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, worldUv * _DetailScale + _DetailOffset.xy);
                half4 color = lerp(layer, detail, _DetailStrength);
                color.rgb = pow(saturate(color.rgb), _HighlightContrast);

                return color * _Tint;
            }
            ENDHLSL
        }
    }
}
