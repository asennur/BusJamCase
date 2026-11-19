// Inverted hull outline: köşeleri normal yönünde şişirir, ön yüzleri atar. Skinned mesh'te de çalışır.
Shader "BusJam/OutlineHull"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.08, 0.08, 0.12, 1)
        _Thickness ("Thickness", Range(0, 0.1)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Thickness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 position = input.positionOS.xyz + normalize(input.normalOS) * _Thickness;
                output.positionCS = TransformObjectToHClip(position);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
