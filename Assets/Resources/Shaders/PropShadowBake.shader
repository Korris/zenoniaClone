// Prop cast-shadow bake for RO3 2.5D maps (PropShadowSilhouettes, once per map load, into a 1 texel / world unit R8 map texture):
// pass 0 draws a standing prop's sprite as a silhouette sheared along the sun onto the ground (alpha above a threshold x _ShadowAlpha, BlendOp Max,
//        so overlapping trees keep the darker one, never stack); the leaf gaps of a crown stay as gaps (dappled shadow);
// pass 1 is one direction of a small gaussian blur (_BlurDir in texels): soft penumbra, no hard sprite edge;
// pass 2 max-combines a texture (the contact ellipses of StaticPropShadows) into the target.
Shader "Hidden/ZenoniaProto/PropShadowBake"
{
    Properties
    {
        _MainTex ("Source", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            BlendOp Max
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _ShadowSrc;
            float _ShadowAlpha;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            half4 frag(v2f i) : SV_Target
            {
                float a = smoothstep(0.3, 0.7, tex2D(_ShadowSrc, i.uv).a) * _ShadowAlpha;
                return half4(a, a, a, a);
            }
            ENDCG
        }

        Pass
        {
            Blend Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _BlurDir; // xy = direction in texels
            half4 frag(v2f_img i) : SV_Target
            {
                float2 d = _BlurDir.xy * _MainTex_TexelSize.xy;
                // 9 taps, sigma ~2 texels
                float s = tex2D(_MainTex, i.uv).r * 0.2270;
                s += (tex2D(_MainTex, i.uv + d * 1.3846).r + tex2D(_MainTex, i.uv - d * 1.3846).r) * 0.3162;
                s += (tex2D(_MainTex, i.uv + d * 3.2308).r + tex2D(_MainTex, i.uv - d * 3.2308).r) * 0.0703;
                return half4(s, s, s, s);
            }
            ENDCG
        }

        Pass
        {
            BlendOp Max
            Blend One One
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            half4 frag(v2f_img i) : SV_Target
            {
                float a = tex2D(_MainTex, i.uv).r;
                return half4(a, a, a, a);
            }
            ENDCG
        }
    }
}
