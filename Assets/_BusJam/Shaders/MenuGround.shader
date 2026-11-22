// Menü çimeni: vertex alfasıyla kenarı yumuşayan düz zemin; ortak toon ışığı vertex'te.
Shader "BusJam/Menu Ground"
{
    Properties
    {
        _BaseColor("Grass", Color) = (.13,.29,.12,1)
        _ShadeColor ("Toon Shade Tint", Color) = (0.72, 0.7, 0.88, 1)
        _RampThreshold ("Toon Ramp Threshold", Range(0, 1)) = 0.45
        _RampSmooth ("Toon Ramp Smooth", Range(0.001, 0.5)) = 0.06
        _AmbientStrength ("Toon Ambient", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "ToonLighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadeColor; half _RampThreshold, _RampSmooth, _AmbientStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; half alpha:TEXCOORD0; half3 light:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            Varyings Vert(Attributes input)
            {
                Varyings output; UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input,output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz); output.alpha=input.color.a;
                output.light=ToonVertexLight(half3(0, 1, 0), _ShadeColor.rgb, _RampThreshold, _RampSmooth, _AmbientStrength); return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return half4(_BaseColor.rgb*input.light,input.alpha*_BaseColor.a);
            }
            ENDHLSL
        }
    }
}
