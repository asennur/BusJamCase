Shader "BusJam/World Ground"
{
    Properties
    {
        _MainTex ("Artwork", 2D) = "white" {}
        [HideInInspector] _Origin ("Reference Position", Vector) = (0,25.37662,-13.83331,0)
        [HideInInspector] _Right ("Reference Right", Vector) = (1,0,0,0)
        [HideInInspector] _Up ("Reference Up", Vector) = (0,.422618,.906308,0)
        [HideInInspector] _Forward ("Reference Forward", Vector) = (0,-.906308,.422618,0)
        [HideInInspector] _Projection ("Reference Projection", Vector) = (.204733,.363970,0,0)
        [HideInInspector] _Artwork ("Artwork Frame", Vector) = (-.150332,-.3,1.300664,1.3)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry-10" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _Origin, _Right, _Up, _Forward, _Projection, _Artwork;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 world : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output; output.world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.world); return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 relative = input.world - _Origin.xyz;
                float depth = max(.001, dot(relative, _Forward.xyz));
                float2 screen = float2(dot(relative, _Right.xyz), dot(relative, _Up.xyz)) / (depth * _Projection.xy) * .5 + .5;
                float2 uv = (screen - _Artwork.xy) / _Artwork.zw;
                // Mirrored edge strips continue asphalt, curb and grass without a visible mesh edge.
                uv.x = 1 - abs(frac(uv.x * .5) * 2 - 1);
                if (uv.y < 0) uv.y = (1 - abs(frac(-uv.y / .24) * 2 - 1)) * .12;
                if (uv.y > 1)
                {
                    float extra = uv.y - 1;
                    half4 edge = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x, 1 - min(extra, .04)));
                    float grass = (1 - abs(frac(extra / .2) * 2 - 1)) * .1 + .015;
                    half4 extension = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x, grass));
                    return lerp(edge, extension, smoothstep(0, .04, extra));
                }
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            }
            ENDHLSL
        }
    }
}
