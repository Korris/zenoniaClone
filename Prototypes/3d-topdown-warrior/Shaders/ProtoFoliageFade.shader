// Translucent twin of Proto/Foliage that OcclusionFade swaps in (alpha from _Color.a) while a tree or bush stands between camera and hero.
Shader "Proto/FoliageFade"
{
    Properties { _Color ("Tint", Color) = (1,1,1,1) _Sway ("Sway (m at tip)", Float) = 0 _Wrap ("Wrap light", Range(0,1)) = 0.45 [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2 }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Cull [_Cull]
        ZWrite Off
        CGPROGRAM
        #pragma surface surf Wrap vertex:vert alpha:fade
        #pragma target 3.0
        #include "ProtoFoliage.cginc"
        ENDCG
    }
}
