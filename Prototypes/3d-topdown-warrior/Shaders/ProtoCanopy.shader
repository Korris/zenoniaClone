// Tripo trees and bushes with volume shading on the leaves (see ProtoCanopy.cginc).
Shader "Proto/Canopy"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1)
        [Normal] _BumpMap ("Normal", 2D) = "bump" {} _BumpScale ("Normal scale", Float) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 0.03 _MinY ("Crown min Y (object)", Float) = 0 _MaxY ("Crown max Y (object)", Float) = 1 _UpObj ("Up (object)", Vector) = (0,1,0,0) _Moss ("Moss", Range(0,1)) = 0 _Dither ("Dither coverage", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        #include "ProtoCanopy.cginc"
        ENDCG
    }
    FallBack "Diffuse"
}
