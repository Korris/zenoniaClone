// Alpha-tested, double-sided leaf / grass cards (ChatGPT v9: cutoff 0.45, casts cut-out shadows).
Shader "Proto/Leaf"
{
    Properties { _MainTex ("Leaf atlas", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1) _Cutoff ("Cutoff", Range(0,1)) = 0.45
                 _Sway ("Sway (m at tip)", Float) = 0 _Wrap ("Wrap light", Range(0,1)) = 0.5 }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Wrap vertex:vert addshadow fullforwardshadows
        #pragma multi_compile_instancing
        #pragma target 3.0
        #include "ProtoLeaf.cginc"
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
