// Translucent twin of Proto/Canopy that OcclusionFade swaps in (alpha from _Color.a) for foreground / occluding trees.
Shader "Proto/CanopyFade"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1)
        [Normal] _BumpMap ("Normal", 2D) = "bump" {} _BumpScale ("Normal scale", Float) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 0.03 _MinY ("Crown min Y (object)", Float) = 0 _MaxY ("Crown max Y (object)", Float) = 1 _UpObj ("Up (object)", Vector) = (0,1,0,0) _Moss ("Moss", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        CGPROGRAM
        #pragma surface surf Standard vertex:vert alpha:fade
        #pragma target 3.0
        #include "ProtoCanopy.cginc"
        ENDCG
    }
}
