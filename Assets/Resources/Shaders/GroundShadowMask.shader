// Ground shadow mask (FakeSun, GroundShadowRenderer): writes shadow strength into the red channel of a low-res mask with BlendOp Max,
// so overlapping shadows keep the darkest one instead of stacking. Pass 0: dynamic blobs (uv -1..1, vertex colour a = alpha, r = feather).
// Pass 1: the static prop-shadow cache texture (red = alpha) over the map rect.
Shader "Hidden/ZenoniaProto/GroundShadowMask"
{
    Properties
    {
        _MainTex ("Prop shadow cache", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        BlendOp Max
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }
            half4 frag(v2f i) : SV_Target
            {
                float d = length(i.uv);
                float k = saturate((1.0 - d) / max(0.05, i.color.r));
                float a = i.color.a * k * k * (3.0 - 2.0 * k);
                return half4(a, a, a, a);
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            half4 frag(v2f i) : SV_Target
            {
                float a = tex2D(_MainTex, i.uv).r;
                return half4(a, a, a, a);
            }
            ENDCG
        }
    }
}
