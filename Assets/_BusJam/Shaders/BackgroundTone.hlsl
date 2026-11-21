#ifndef BUSJAM_BACKGROUND_TONE_INCLUDED
#define BUSJAM_BACKGROUND_TONE_INCLUDED
// Shared globals are consumed only by the gameplay background shaders.
half4 _BusJamBackgroundTone;
half4 _BusJamBackgroundHaze;
half3 MuteBackground(half3 color)
{
    half luminance = dot(color, half3(0.2126, 0.7152, 0.0722));
    color = lerp(color, luminance.xxx, _BusJamBackgroundTone.x);
    color = lerp(color, half3(0.5, 0.5, 0.5), _BusJamBackgroundTone.z);
    return lerp(color, _BusJamBackgroundHaze.rgb, _BusJamBackgroundTone.y);
}
#endif
