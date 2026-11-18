// Translucent twin of Proto/Leaf that OcclusionFade swaps in (alpha from _Color.a) while a crown stands between camera and hero.
Shader "Proto/LeafFade"
{
    Properties { _MainTex ("Leaf atlas", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1) _Cutoff ("Cutoff", Range(0,1)) = 0.45
                 _Sway ("Sway (m at tip)", Float) = 0 _Wrap ("Wrap light", Range(0,1)) = 0.5 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        CGPROGRAM
        #pragma surface surf Wrap vertex:vert alpha:fade
        #pragma target 3.0
        #include "ProtoLeaf.cginc"
        ENDCG
    }
}
