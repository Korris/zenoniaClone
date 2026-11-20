// Soft drop shadow for the 2.5D prototype (View25D): a sprite's silhouette (alpha only, 5-tap blur) in black, lying flat on the ground.
// SpriteRenderer feeds the sprite texture and the vertex colour (alpha = shadow strength).
Shader "ZenoniaProto/View25DShadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Blur ("Blur (texels)", Float) = 2.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Blur;

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
                float2 d = _MainTex_TexelSize.xy * _Blur;
                float a = tex2D(_MainTex, i.uv).a * 0.4
                        + (tex2D(_MainTex, i.uv + float2(d.x, 0)).a + tex2D(_MainTex, i.uv - float2(d.x, 0)).a
                         + tex2D(_MainTex, i.uv + float2(0, d.y)).a + tex2D(_MainTex, i.uv - float2(0, d.y)).a) * 0.15;
                return half4(i.color.rgb, a * i.color.a);
            }
            ENDCG
        }
    }
}
