Shader "Game/SubmergedSprite"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
        [PerRendererData] _SubmersionMask("Submersion Mask", 2D) = "black" {}
        [PerRendererData] _UseSubmersionMask("Use Submersion Mask", Float) = 0
        _Color("Tint", Color) = (1,1,1,1)
        _RedHullThreshold("Red Hull Threshold", Range(0,1)) = 0.08
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
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_SubmersionMask);
            SAMPLER(sampler_SubmersionMask);
            float4 _Color;
            float _UseSubmersionMask;
            float _RedHullThreshold;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                half mask = SAMPLE_TEXTURE2D(_SubmersionMask, sampler_SubmersionMask, input.uv).r;
                half redHull = saturate(color.r - max(color.g, color.b) - _RedHullThreshold);
                half submersion = lerp(step(0.02, redHull), mask, _UseSubmersionMask);
                color.a *= 1.0 - submersion;
                return color;
            }
            ENDHLSL
        }
    }
}
