Shader "BusJam/Menu Ground"
{
    Properties { _BaseColor("Grass", Color) = (.13,.29,.12,1) }
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; half alpha:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            Varyings Vert(Attributes input)
            {
                Varyings output; UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input,output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz); output.alpha=input.color.a; return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Light light=GetMainLight();
                half3 lighting=SampleSH(half3(0,1,0))+light.color*saturate(light.direction.y);
                return half4(_BaseColor.rgb*lighting,input.alpha*_BaseColor.a);
            }
            ENDHLSL
        }
    }
}
