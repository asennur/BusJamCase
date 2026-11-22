// Menü kayan yolu: doku ve orta şerit _MenuMotion ile kayar, ortak toon ışığı.
Shader "BusJam/Menu Moving Road"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _ShadeColor ("Shade Tint", Color) = (0.72, 0.7, 0.88, 1)
        _RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.45
        _RampSmooth ("Ramp Smooth", Range(0.001, 0.5)) = 0.06
        _AmbientStrength ("Ambient", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "MenuForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #define MENU_ROAD 1
            #include "MenuMoving.hlsl"
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
