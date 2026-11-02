// UI twin of SpriteShine: the light band over an enhanced item's icon, from the same mask (R = how much light, G = position along the item, A = silhouette).
// Shared per enhance band, so _Shine sits on the material: a canvas image cannot take a property block.
Shader "ZenoniaProto/UiShine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Shine Mask", 2D) = "white" {}
        _Shine ("Band centre, half width, lift", Vector) = (-2,0.1,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Shine;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 m = tex2D(_MainTex, i.uv);
                half band = saturate(1.0 - abs(m.g - _Shine.x) / _Shine.y);
                band = band * band * (3.0 - 2.0 * band);
                return half4(i.color.rgb * m.r * (band + _Shine.z), i.color.a * m.a);
            }
            ENDCG
        }
    }
}
