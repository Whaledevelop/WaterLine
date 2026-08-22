Shader "Game/WakeDecal"
{
    Properties
    {
        _WakeTexA("Wake Texture A", 2D) = "white" {}
        _WakeTexB("Wake Texture B", 2D) = "white" {}
        _Tint("Tint", Color) = (0.92,0.985,1,1)
        _FlowSpeed("Flow Speed", Float) = 0.015
        _AlphaPower("Alpha Power", Range(0.25,3)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
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
                float2 variant : TEXCOORD1;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float variant : TEXCOORD1;
                float4 color : COLOR;
            };

            TEXTURE2D(_WakeTexA);
            SAMPLER(sampler_WakeTexA);
            TEXTURE2D(_WakeTexB);
            SAMPLER(sampler_WakeTexB);
            float4 _Tint;
            float _FlowSpeed;
            float _AlphaPower;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.variant = input.variant.x;
                output.color = input.color;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 first = SAMPLE_TEXTURE2D(_WakeTexA, sampler_WakeTexA, input.uv);
                half4 second = SAMPLE_TEXTURE2D(_WakeTexB, sampler_WakeTexB, input.uv);
                half4 wake = lerp(first, second, step(0.5, input.variant));
                half longitudinalFade = smoothstep(0.0, 0.16, input.uv.x) *
                    smoothstep(0.0, 0.16, 1.0 - input.uv.x);
                half alpha = pow(saturate(wake.a), _AlphaPower) * longitudinalFade * input.color.a * _Tint.a;

                return half4(wake.rgb * _Tint.rgb * input.color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
