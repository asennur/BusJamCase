Shader "BusJam/Scrolling Water"
{
    Properties
    {
        _BaseMap ("Water", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _ScrollSpeed ("UV speed", Vector) = (0.015,0.025,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../../Shaders/BackgroundTone.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST, _BaseColor, _ScrollSpeed;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output; UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input,output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=TRANSFORM_TEX(input.uv,_BaseMap)+_Time.y*_ScrollSpeed.xy; return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 color = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv)*_BaseColor;
                color.rgb = MuteBackground(color.rgb);
                return color;
            }
            ENDHLSL
        }
    }
}
