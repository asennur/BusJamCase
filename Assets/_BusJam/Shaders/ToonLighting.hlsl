#ifndef BUSJAM_TOON_LIGHTING_INCLUDED
#define BUSJAM_TOON_LIGHTING_INCLUDED
// Ortak toon ışığı: ana ışıkla yarım-Lambert tek basamak (iki ton) + SH ambient.
// Toon, zemin, su ve menü shader'ları aynı modeli kullanır; alanlar her shader'ın UnityPerMaterial'ında.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// 0 = gölge tonu, 1 = aydınlık.
half ToonStep(half3 normalWS, half3 lightDir, half threshold, half smoothness)
{
    half ndl = dot(normalWS, lightDir) * 0.5h + 0.5h;
    return smoothstep(threshold - smoothness, threshold + smoothness, ndl);
}

// Albedo ile çarpılan ışık: basamaklı ana ışık + SH ambient.
half3 ToonDiffuse(half lit, half3 shadeColor, half3 lightColor, half3 ambient)
{
    return lerp(shadeColor, 1.0h, lit) * lightColor + ambient;
}

// Düz/az eğimli yüzeyler (zemin, su, kaldırım) için tüm ışık vertex'te: normal tüm yüzeyde aynı.
half3 ToonVertexLight(half3 normalWS, half3 shadeColor, half threshold, half smoothness, half ambientStrength)
{
    Light light = GetMainLight();
    half lit = ToonStep(normalWS, light.direction, threshold, smoothness);
    return ToonDiffuse(lit, shadeColor, light.color, SampleSH(normalWS) * ambientStrength);
}
#endif
