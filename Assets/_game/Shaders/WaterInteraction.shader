Shader "Game/WaterInteraction"
{
    Properties
    {
        _FoamTex("Foam Texture", 2D) = "white" {}
        _Tint("Tint", Color) = (0.92,0.985,1,1)
        _TextureScale("Texture Scale", Vector) = (1,1,0,0)
        _FlowSpeed("Flow Speed", Float) = 0.08
        _TextureThreshold("Texture Threshold", Range(0,1)) = 0.58
        _TextureSoftness("Texture Softness", Range(0.01,0.5)) = 0.16
        _EdgeSoftness("Edge Softness", Range(0.01,1)) = 0.12
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
            float4 _TextureScale;
            float _FlowSpeed;
            float _TextureThreshold;
            float _TextureSoftness;
            float _EdgeSoftness;

            TEXTURE2D(_FoamTex);
            SAMPLER(sampler_FoamTex);

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
                float2 foamUv = input.uv * _TextureScale.xy;
                foamUv.x -= _Time.y * _FlowSpeed;
                half3 foamColor = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, foamUv).rgb;
                half foam = dot(foamColor, half3(0.299, 0.587, 0.114));
                foam = smoothstep(_TextureThreshold - _TextureSoftness, _TextureThreshold + _TextureSoftness, foam);
                half edge = smoothstep(0, _EdgeSoftness, input.uv.y) * smoothstep(0, _EdgeSoftness, 1 - input.uv.y);
                half center = 1 - abs(input.uv.y * 2 - 1);
                half streaks = lerp(0.35, 1, smoothstep(0.08, 0.9, center));
                half alpha = input.color.a * foam * edge * streaks;

                return half4(_Tint.rgb * input.color.rgb, alpha * _Tint.a);
            }
            ENDHLSL
        }
    }
}
