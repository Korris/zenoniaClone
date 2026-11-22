// Replacement shader of the one-off sun shadow bake of a 3D map (Map3DBakedShadow, graphics quality Medium, 22/11 perf pass): writes the depth
// from the fixed sun (row 3 of _M3DBakedVP, 0..1) into an RFloat target, nearest wins by the z-buffer; picked by RenderType. Opaque = trunks, rocks, ruins, wood, terrain hills; TransparentCutout = leaf cards (alpha cutout as their
// ShadowCaster pass, and only the cards of the 6 casting atlas cells when the material has _CastCells = 1: Map3DFoliage) and cutout sprites.
// No wind: the bake is static. Cull Off: thin cards and open meshes cast from both sides.
Shader "Hidden/ZenoniaProto/Map3DShadowBake"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Cutoff ("Cutoff", Float) = 0.5
        _CastCells ("Cast cells only", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4x4 _M3DBakedVP;
            struct v2f { float4 pos : SV_POSITION; float d : TEXCOORD0; };
            v2f vert(float4 vertex : POSITION) { v2f o; o.pos = UnityObjectToClipPos(vertex); o.d = mul(_M3DBakedVP, mul(unity_ObjectToWorld, vertex)).z; return o; }
            float4 frag(v2f i) : SV_Target { return float4(i.d, 0, 0, 0); }
            ENDCG
        }
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" }
        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float _Cutoff, _CastCells;
            float4x4 _M3DBakedVP;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float d : TEXCOORD1; };
            v2f vert(float4 vertex : POSITION, float2 uv : TEXCOORD0)
            {
                v2f o; o.pos = UnityObjectToClipPos(vertex); o.uv = uv; o.d = mul(_M3DBakedVP, mul(unity_ObjectToWorld, vertex)).z; return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                clip(tex2D(_MainTex, i.uv).a * (_CastCells > 0.5 ? 1.25 : 1.0) - _Cutoff);
                float2 cell = floor(i.uv * 4.0);
                if (_CastCells > 0.5) clip(0.3 - frac((cell.y * 4.0 + cell.x) * 0.618034)); // Map3DMeshKit.CastsShadow
                return float4(i.d, 0, 0, 0);
            }
            ENDCG
        }
    }
}
