// Yolcu altındaki yumuşak temas gölgesi: quad UV'sinden radyal düşüş, doku yok. Saydam, ZWrite kapalı, tek pass.
Shader "BusJam/ContactShadow"
{
    Properties
    {
        _BaseColor ("Color (alpha = strength)", Color) = (0.25, 0.2, 0.4, 0.35)
        _Softness ("Softness", Range(0.05, 1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-50" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ContactShadow"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Softness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * 2.0 - 1.0;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half r = length(input.uv);
                half a = 1.0 - smoothstep(1.0 - _Softness, 1.0, r);
                return half4(_BaseColor.rgb, _BaseColor.a * a);
            }
            ENDHLSL
        }
    }
}
