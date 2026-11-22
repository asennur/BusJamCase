// Ters hull kontur: arka yüzleri ekran düzleminde normal yönünde iter (clip-space), ön yüzleri atar.
// Kalınlık dünya biriminde (karakterle orantılı), uzak kamerada piksel alt sınırı. Skinned mesh'te de çalışır.
Shader "BusJam/OutlineHull"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.137, 0.106, 0.11, 1)
        _Width ("Width (world)", Range(0, 0.2)) = 0.085
        _MinPixels ("Min Width (px @1920)", Range(0, 10)) = 2.5
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
                float _Width;
                float _MinPixels;
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
                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float2 dir = mul((float3x3)UNITY_MATRIX_VP, TransformObjectToWorldNormal(input.normalOS)).xy;
                dir *= rsqrt(max(dot(dir, dir), 1e-8));

                // Clip biriminde: dünya kalınlığı mesafeyle küçülür, piksel alt sınırı w ile büyür. RT çiziminde P ters dönebilir → abs.
                float width = max(_Width * abs(UNITY_MATRIX_P._m11), _MinPixels * (2.0 / 1920.0) * positionCS.w);
                positionCS.xy += dir * float2(_ScreenParams.y / _ScreenParams.x, 1.0) * width;
                output.positionCS = positionCS;
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
