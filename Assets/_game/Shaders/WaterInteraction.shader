Shader "Game/WaterInteraction"
{
    Properties
    {
        _Tint("Tint", Color) = (0.86,0.97,1,1)
        _NoiseScale("Noise Scale", Float) = 2.8
        _NoiseSpeed("Noise Speed", Vector) = (0.08,0.025,0,0)
        _EdgeSoftness("Edge Softness", Range(0.01,1)) = 0.22
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
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 color : COLOR;
            };

            float4 _Tint;
            float4 _NoiseSpeed;
            float _NoiseScale;
            float _EdgeSoftness;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 noiseUv = input.positionWS.xy * _NoiseScale + _Time.y * _NoiseSpeed.xy;
                half noise = sin(noiseUv.x * 2.13 + sin(noiseUv.y * 1.71));
                noise = noise * 0.5 + 0.5;
                half edge = smoothstep(0, _EdgeSoftness, input.uv.y) * smoothstep(0, _EdgeSoftness, 1 - input.uv.y);
                half alpha = input.color.a * lerp(0.35, 1, noise) * edge;

                return half4(_Tint.rgb * input.color.rgb, alpha * _Tint.a);
            }
            ENDHLSL
        }
    }
}
