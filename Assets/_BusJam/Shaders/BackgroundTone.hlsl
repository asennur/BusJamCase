#ifndef BUSJAM_BACKGROUND_TONE_INCLUDED
#define BUSJAM_BACKGROUND_TONE_INCLUDED
// Shared globals are consumed only by the gameplay background shaders.
half4 _BusJamBackgroundTone;
half3 MuteBackground(half3 color)
{
    half luminance = dot(color, half3(0.2126, 0.7152, 0.0722));
    color = lerp(color, luminance.xxx, _BusJamBackgroundTone.x);
    // Compress brighter tones gently; black stays black, with no additive lift.
    color /= 1.0h + luminance * _BusJamBackgroundTone.z;
    return color * (1.0h - _BusJamBackgroundTone.y);
}
#endif
