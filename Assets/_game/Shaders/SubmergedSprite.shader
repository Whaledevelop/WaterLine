Shader "Game/SubmergedSprite"
{
    Properties
    {
        _MainTex("Sprite", 2D) = "white" {}
        [PerRendererData] _SubmersionMask("Submersion Mask", 2D) = "black" {}
        [PerRendererData] _UseSubmersionMask("Use Submersion Mask", Float) = 0
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("Renderer Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
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
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            TEXTURE2D(_SubmersionMask);
            SAMPLER(sampler_SubmersionMask);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            float _UseSubmersionMask;

            Varyings Vert(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = CommonUnlitFragment(input, input.color);
                half mask = SAMPLE_TEXTURE2D(_SubmersionMask, sampler_SubmersionMask, input.uv).r;
                color.a *= 1 - mask * _UseSubmersionMask;

                return color;
            }
            ENDHLSL
        }
    }
}
