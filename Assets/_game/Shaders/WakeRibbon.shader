Shader "Game/WakeRibbon"
{
    Properties
    {
        _WakeTex("Wake Texture", 2D) = "white" {}
        _Tint("Tint", Color) = (0.92,0.985,1,0.8)
        _AlphaPower("Alpha Power", Range(0.25,3)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent+19" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
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

            TEXTURE2D(_WakeTex);
            SAMPLER(sampler_WakeTex);
            float4 _Tint;
            float _AlphaPower;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 wake = SAMPLE_TEXTURE2D(_WakeTex, sampler_WakeTex, input.uv);
                half alpha = pow(saturate(wake.a), _AlphaPower) * input.color.a * _Tint.a;

                return half4(wake.rgb * _Tint.rgb * input.color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
