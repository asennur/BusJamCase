// Prosedürel oyun zemini (karo, kaldırım, yol, çimen); düz yüzey, ortak toon ışığı vertex'te.
// Arka plan kısma (MuteBackground) yalnız mor karo ve yolda; kaldırım ve çimen referanstaki gibi açık kalır.
Shader "BusJam/Modular Ground"
{
    Properties
    {
        _TileLight ("Light tile", Color) = (0.55,0.45,0.79,1)
        _TileDark ("Dark tile", Color) = (0.45,0.37,0.70,1)
        _Grout ("Grout", Color) = (0.30,0.25,0.52,1)
        _Sidewalk ("Sidewalk", Color) = (0.77,0.72,0.87,1)
        _SidewalkSpot ("Sidewalk Spot", Color) = (0.70,0.74,0.93,1)
        _Road ("Road", Color) = (0.47,0.40,0.74,1)
        _Grass ("Grass", Color) = (0.61,0.86,0.12,1)
        _Curb ("Top Curb", Color) = (0.96,0.93,0.86,1)
        _CurbTop ("Top Curb upper z (grass above)", Float) = 13.01
        _RoadEdge ("Road Edge Stripe", Color) = (0.84,0.53,0.43,1)
        _RoadEdgeGutter ("Road Edge Gutter", Color) = (0.36,0.31,0.45,1)
        _RoadEdgeZ ("Road Edge z (bottom, top, half width, gutter half width)", Vector) = (8.98,12.65,0.05,0.09)
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
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "ToonLighting.hlsl"
            #include "BackgroundTone.hlsl"
            #include "SidewalkSpots.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 band:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 ground:TEXCOORD0; float band:TEXCOORD1; half3 light:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID };
            CBUFFER_START(UnityPerMaterial)
                half4 _TileLight, _TileDark, _Grout, _Sidewalk, _SidewalkSpot, _Road, _Grass, _Curb, _RoadEdge, _RoadEdgeGutter;
                float4 _RoadEdgeZ; float _CurbTop;
                half4 _ShadeColor; half _RampThreshold, _RampSmooth, _AmbientStrength;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input,output);
                float3 world=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(world);
                output.ground=world.xz; output.band=input.band.x;
                output.light=ToonVertexLight(half3(0, 1, 0), _ShadeColor.rgb, _RampThreshold, _RampSmooth, _AmbientStrength); return output;
            }
            float Stripe(float coordinate, float center, float halfWidth)
            {
                float aa=max(fwidth(coordinate),0.0001);
                return 1-smoothstep(halfWidth-aa,halfWidth+aa,abs(coordinate-center));
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float2 p=input.ground;
                half3 color;
                bool mute=true;
                float gutter=0, edge=0;
                if(input.band<0.5)
                {
                    // The source has roughly 18 columns and 70-pixel rows on its 45-unit quad.
                    float2 grid=float2((p.x+22.5)/2.43,(4.703254-p.y)/2.50);
                    float2 id=floor(grid), f=frac(grid);
                    float hash=frac(sin(dot(id,float2(127.1,311.7)))*43758.5453);
                    color=lerp(_TileDark.rgb,_TileLight.rgb,hash);
                    color*=0.97+0.05*(f.x*.4+f.y*.6);
                    float2 q=abs(f-.5)-.425;
                    float border=length(max(q,0))+min(max(q.x,q.y),0)-.06;
                    float aa=max(fwidth(border),0.0001);
                    float inside=1-smoothstep(-aa,aa,border);
                    float bevel=(1-smoothstep(.005,.022,-border))*.10;
                    color=lerp(_Grout.rgb,color*(1+bevel),inside);
                }
                else if(input.band<1.5) { color=SidewalkSpots(p,_Sidewalk.rgb,_SidewalkSpot.rgb); mute=false; }
                else if(input.band<2.5)
                {
                    color=_Road.rgb*(0.97+0.035*sin(p.y*0.8));
                    // Alt ve üst kenarda koyu oluk içinde ince turuncu şerit (ref t0030).
                    gutter=max(Stripe(p.y,_RoadEdgeZ.x,_RoadEdgeZ.w),Stripe(p.y,_RoadEdgeZ.y,_RoadEdgeZ.w));
                    edge=max(Stripe(p.y,_RoadEdgeZ.x,_RoadEdgeZ.z),Stripe(p.y,_RoadEdgeZ.y,_RoadEdgeZ.z));
                    float along=abs(frac(p.x/4.80)-.5)*4.80;
                    float distance=length(float2(max(along-.89,0),p.y-10.768));
                    float dash=1-smoothstep(.092-fwidth(distance),.092+fwidth(distance),distance);
                    color=lerp(color,half3(.96,.96,1),dash);
                }
                else if(input.band<3.5) { color=_Grass.rgb; mute=false; }
                else { color=p.y<_CurbTop?_Curb.rgb:_Grass.rgb; mute=false; }
                color*=input.light;
                if(mute) color=MuteBackground(color);
                // Şerit kısılmaz: referans turuncusu yol tonundan bağımsız.
                color=lerp(lerp(color,_RoadEdgeGutter.rgb,gutter),_RoadEdge.rgb*input.light,edge);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
