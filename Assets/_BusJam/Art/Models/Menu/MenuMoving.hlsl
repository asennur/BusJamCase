#ifndef BUSJAM_MENU_MOVING_INCLUDED
#define BUSJAM_MENU_MOVING_INCLUDED
// Menü kayan dünyası: dekor uzakta yumuşakça belirir, yol dokusu ve şeridi _MenuMotion ile kayar.
// Işık ortak toon modeli; MENU_ROAD bir #define (keyword değil), iki shader dosyası bu gövdeyi paylaşır.
#include "../../../Shaders/ToonLighting.hlsl"

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _ShadeColor;
    half _RampThreshold, _RampSmooth, _AmbientStrength;
CBUFFER_END
float4 _MenuMotion;

struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    half3 normalWS : TEXCOORD1;
    half3 ambient : TEXCOORD2;
    float2 uv : TEXCOORD3;
};

Varyings Vert(Attributes input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    Varyings output;
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.ambient = SampleSH(output.normalWS) * _AmbientStrength;
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}

half4 Frag(Varyings input) : SV_Target
{
    float2 uv = input.uv;
    half alpha = 1.0h;
#ifdef MENU_ROAD
    float2 road = input.positionWS.xz - _MenuMotion.yz;
    uv = float2(road.x, road.y - _MenuMotion.x) * .5;
#else
    // Havuzdaki dekor değişimi ufukta, geri dönüşümden önce yavaşça gizlenir.
    alpha = smoothstep(-39, -29, input.positionWS.z - _MenuMotion.z);
    clip(alpha - .0001);
#endif
    half3 albedo = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
#ifdef MENU_ROAD
    float x = abs(road.x);
    float dash = abs(frac((road.y - _MenuMotion.x) / 3) - .5) * 3;
    float lane = (1 - smoothstep(.06 - fwidth(x), .06 + fwidth(x), x))
        * (1 - smoothstep(.7 - fwidth(dash), .7 + fwidth(dash), dash));
    albedo = lerp(albedo, half3(.94, .94, .94), lane);
#endif
    Light light = GetMainLight();
    half3 n = normalize(input.normalWS);
    half lit = ToonStep(n, light.direction, _RampThreshold, _RampSmooth);
    return half4(albedo * ToonDiffuse(lit, _ShadeColor.rgb, light.color, input.ambient), alpha);
}
#endif
