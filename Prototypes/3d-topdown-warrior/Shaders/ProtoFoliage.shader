// Opaque foliage (trees, bushes, grass). _Cull 0 = double-sided (grass blades), 2 = back-face culling (canopies).
Shader "Proto/Foliage"
{
    Properties { _Color ("Tint", Color) = (1,1,1,1) _Sway ("Sway (m at tip)", Float) = 0 _Wrap ("Wrap light", Range(0,1)) = 0.45 [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull [_Cull]
        CGPROGRAM
        #pragma surface surf Wrap vertex:vert addshadow fullforwardshadows
        #pragma multi_compile_instancing
        #pragma target 3.0
        #include "ProtoFoliage.cginc"
        ENDCG
    }
    FallBack "Diffuse"
}
