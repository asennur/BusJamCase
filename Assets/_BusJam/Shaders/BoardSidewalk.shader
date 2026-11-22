// Tahta arkası ve karo altları kaldırım yüksekliğine düzlenir; ortak toon ışığı (yukarı normal) vertex'te.
Shader "BusJam/Board Sidewalk"
{
    Properties
    {
        _Sidewalk ("Sidewalk (match Modular Ground)", Color) = (0.80,0.735,0.895,1)
        _GroundHeight ("Sidewalk world height", Float) = -0.30
        _ShadeColor ("Toon Shade Tint", Color) = (0.72, 0.7, 0.88, 1)
        _RampThreshold ("Toon Ramp Threshold", Range(0, 1)) = 0.45
        _RampSmooth ("Toon Ramp Smooth", Range(0.001, 0.5)) = 0.06
        _AmbientStrength ("Toon Ambient", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            // Resolve coplanar floor coverage without introducing a physical height step.
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "ToonLighting.hlsl"
            #include "BackgroundTone.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Sidewalk;
                float _GroundHeight;
                half4 _ShadeColor; half _RampThreshold, _RampSmooth, _AmbientStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half3 light : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input,output);
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                // The board backing and tile undersides meet the sidewalk without a height step.
                world.y = _GroundHeight;
                output.positionCS = TransformWorldToHClip(world);
                output.light = ToonVertexLight(half3(0, 1, 0), _ShadeColor.rgb, _RampThreshold, _RampSmooth, _AmbientStrength);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return half4(MuteBackground(_Sidewalk.rgb * input.light),1);
            }
            ENDHLSL
        }
    }
}
