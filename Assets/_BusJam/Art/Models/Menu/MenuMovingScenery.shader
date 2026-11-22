// Menü kayan dekoru: ortak toon ışığı, ufukta alfa ile belirir.
Shader "BusJam/Menu Moving Toon"
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
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "MenuForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "MenuMoving.hlsl"
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
