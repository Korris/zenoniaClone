// Bake of the tree impostor atlases (Map3DImpostorBaker, 22/11 perf pass 2), drawn with a command buffer into the atlas frames.
// Pass 0 = albedo, premultiplied on transparent black: leaves = the Map3DFoliage formula (atlas x tint / tint 2 by vertex a x cluster variation b
// x crown AO r, mip-compensated cutout), bark = the Map3DPainted uv mode (bark x tint x AO r; moss left out, trunks are small at that distance).
// Pass 1 = object-space normal (rgb, n * 0.5 + 0.5) and the surface depth toward the bake camera relative to the sphere centre (a, 0.5 = centre plane,
// +-radius = 0 / 1). No wind, no lighting: the impostor shader lights it.
Shader "Hidden/ZenoniaProto/Map3DImpostorBake"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Cutoff ("Cutoff", Float) = 0.47
        _Tint ("Tint", Color) = (1,1,1,1)
        _Tint2 ("Tint 2", Color) = (1,1,1,1)
        _Leaf ("Leaf (1) or bark (0)", Float) = 1
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex; float4 _MainTex_TexelSize;
    float _Cutoff, _Leaf;
    fixed4 _Tint, _Tint2;
    float4 _ImpBakeView, _ImpBakeCenter;
    struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
    struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 col : TEXCOORD1; float3 n : TEXCOORD2; float3 op : TEXCOORD3; };
    v2f vert(appdata v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv; o.col = v.color; o.n = v.normal; o.op = v.vertex.xyz;
        return o;
    }
    float MipLevel(float2 uv)
    {
        float2 d = max(abs(ddx(uv)), abs(ddy(uv))) * _MainTex_TexelSize.zw;
        return max(0.0, log2(max(d.x, d.y)));
    }
    float3 Albedo(v2f i)
    {
        // 1.5 mips sharper than the frame size asks: the leaf atlas mips fold the dark transparent texels into the colour, the bake came out darker
        // and flatter than the mesh at the same screen size (first try); the coverage compensation keeps using the real mip level
        fixed4 t = tex2Dbias(_MainTex, float4(i.uv, 0, -1.5));
        if (_Leaf > 0.5)
        {
            clip(t.a * (1.0 + MipLevel(i.uv) * 0.22) - _Cutoff);
            fixed3 tint = lerp(_Tint.rgb, _Tint2.rgb, i.col.a);
            float var = 1.0 + (i.col.b - 0.5) * 0.12;
            return t.rgb * tint * var * lerp(0.62, 1.0, i.col.r);
        }
        return t.rgb * _Tint.rgb * lerp(0.72, 1.0, i.col.r);
    }
    ENDCG
    SubShader
    {
        Cull Off ZWrite On ZTest LEqual
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(v2f i) : SV_Target { return fixed4(Albedo(i), 1.0); }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(v2f i) : SV_Target
            {
                Albedo(i); // same cutout
                float3 n = normalize(i.n);
                float d = dot(i.op - _ImpBakeCenter.xyz, _ImpBakeView.xyz) / max(_ImpBakeCenter.w, 1e-3);
                return fixed4(n * 0.5 + 0.5, saturate(d * 0.5 + 0.5));
            }
            ENDCG
        }
    }
}
