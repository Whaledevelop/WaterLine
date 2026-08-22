Shader "Game/WaterlineFoam"
{
    Properties
    {
        _MainTex("Sprite", 2D) = "white" {}
        [PerRendererData] _WaterlineMask("Waterline Mask", 2D) = "black" {}
        [PerRendererData] _Intensity("Intensity", Range(0,1)) = 0
        _Tint("Tint", Color) = (0.88,0.98,1,1)
        _NoiseScale("Noise Scale", Float) = 4.5
        _NoiseSpeed("Noise Speed", Vector) = (0.11,0.035,0,0)
        [HideInInspector] _Color("Renderer Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("Renderer Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+30" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS_SHARED
                float3 positionWS : TEXCOORD1;
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            TEXTURE2D(_WaterlineMask);
            SAMPLER(sampler_WaterlineMask);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _Tint;
                float4 _NoiseSpeed;
                float _NoiseScale;
            CBUFFER_END

            float _Intensity;

            Varyings Vert(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.color = input.color * _Color * unity_SpriteColor;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 mask = SAMPLE_TEXTURE2D(_WaterlineMask, sampler_WaterlineMask, input.uv).rgb;
                float2 noiseUv = input.positionWS.xy * _NoiseScale + _Time.y * _NoiseSpeed.xy;
                half firstNoise = sin(noiseUv.x * 2.17 + sin(noiseUv.y * 1.63));
                half secondNoise = sin(noiseUv.y * 3.11 - noiseUv.x * 0.73);
                half noise = saturate(firstNoise * 0.3 + secondNoise * 0.2 + 0.65);
                half waterline = mask.r * lerp(0.2, 0.42, _Intensity);
                half bowWave = mask.g * _Intensity * 1.35;
                half sternWash = mask.b * _Intensity * 1.12;
                half alpha = saturate(waterline + bowWave + sternWash);
                alpha *= lerp(0.48, 1, noise) * input.color.a;

                return half4(_Tint.rgb, alpha * _Tint.a);
            }
            ENDHLSL
        }
    }
}
