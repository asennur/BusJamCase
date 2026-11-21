// Mobil toon: iki tonlu basamak diffuse + sert specular nokta + üstten rim. Ana ışık + SH ambient, keyword yok.
// SRP Batcher uyumlu (tüm alanlar UnityPerMaterial'da); instancing açık. Kontur ayrı OutlineHull materyalinde.
Shader "BusJam/Toon"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _ShadeColor ("Shade Tint", Color) = (0.72, 0.7, 0.88, 1)
        _RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.45
        _RampSmooth ("Ramp Smooth", Range(0.001, 0.5)) = 0.06
        _AmbientStrength ("Ambient", Range(0, 1)) = 0.25
        _SpecColor ("Specular Color", Color) = (1, 1, 1, 1)
        _SpecSize ("Specular Size", Range(0, 0.5)) = 0.06
        _SpecStrength ("Specular Strength", Range(0, 1)) = 0.55
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimThreshold ("Rim Threshold", Range(0, 1)) = 0.62
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadeColor;
                half4 _SpecColor;
                half4 _RimColor;
                half _RampThreshold;
                half _RampSmooth;
                half _AmbientStrength;
                half _SpecSize;
                half _SpecStrength;
                half _RimThreshold;
                half _RimStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                half3 viewWS : TEXCOORD1;
                half3 ambient : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(positionWS);
                output.ambient = SampleSH(output.normalWS) * _AmbientStrength;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normalWS);
                half3 v = normalize(input.viewWS);
                Light light = GetMainLight();

                // Yarım Lambert'i tek basamakla iki tona böler.
                half ndl = dot(n, light.direction) * 0.5h + 0.5h;
                half lit = smoothstep(_RampThreshold - _RampSmooth, _RampThreshold + _RampSmooth, ndl);
                half3 color = _BaseColor.rgb * (lerp(_ShadeColor.rgb, 1.0h, lit) * light.color + input.ambient);

                // Sert kenarlı Blinn noktası; yalnız aydınlık tarafta. Tam yukarı bakan düz yüzeyler
                // (otobüs tabanı gibi) maskelenir, yoksa tüm yüz beyaza boyanır.
                half ndh = saturate(dot(n, normalize(light.direction + v)));
                half spec = smoothstep(1.0h - _SpecSize, 1.0h - _SpecSize * 0.7h, ndh * ndh) * lit;
                spec *= saturate((0.995h - n.y) * 60.0h);
                color += _SpecColor.rgb * light.color * (spec * _SpecStrength);

                // Silüet kenarında, üste bakan yüzeylerde açık bant.
                half rim = smoothstep(_RimThreshold, _RimThreshold + 0.08h, 1.0h - saturate(dot(n, v))) * saturate(n.y + 0.3h);
                color = lerp(color, _RimColor.rgb, rim * _RimStrength);

                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
