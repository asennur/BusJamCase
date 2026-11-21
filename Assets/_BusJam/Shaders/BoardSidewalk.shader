Shader "BusJam/Board Sidewalk"
{
    Properties
    {
        _Sidewalk ("Sidewalk (match Modular Ground)", Color) = (0.80,0.735,0.895,1)
        _GroundHeight ("Sidewalk world height", Float) = -0.30
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BackgroundTone.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Sidewalk;
                float _GroundHeight;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input,output);
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                // The board backing and tile undersides meet the sidewalk without a height step.
                world.y = _GroundHeight;
                output.positionCS = TransformWorldToHClip(world);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return half4(MuteBackground(_Sidewalk.rgb),1);
            }
            ENDHLSL
        }
    }
}
