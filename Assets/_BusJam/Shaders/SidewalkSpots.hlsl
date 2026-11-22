#ifndef BUSJAM_SIDEWALK_SPOTS_INCLUDED
#define BUSJAM_SIDEWALK_SPOTS_INCLUDED
// Kaldırımdaki seyrek yassı taş lekeleri: dünya xz'de hücre başına en fazla bir oval, alt kenarı biraz koyu.
#define SIDEWALK_SPOT_CELL 1.2

half3 SidewalkSpots(float2 xz, half3 baseColor, half3 spotColor)
{
    float2 cell = xz / SIDEWALK_SPOT_CELL;
    float2 id = floor(cell);
    float h = frac(sin(dot(id, float2(127.1, 311.7))) * 43758.5453);
    float2 center = 0.25 + 0.5 * frac(h * float2(91.7, 37.3));
    // Oval yarıçapları dünya biriminde (0.19 × 0.13); hücre kenarına taşmaz, komşuya bakmak gerekmez.
    float2 d = (frac(cell) - center) * SIDEWALK_SPOT_CELL / float2(0.19, 0.13);
    float r = length(d);
    float aa = max(fwidth(r), 0.0001);
    float spot = (1 - smoothstep(1 - aa, 1 + aa, r)) * step(frac(h * 13.1), 0.55);
    half3 color = spotColor * (1 - 0.06h * smoothstep(0.4, 1.0, r) * saturate(-d.y));
    return lerp(baseColor, color, spot);
}
#endif
