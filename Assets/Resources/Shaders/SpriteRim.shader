// Rim light of an enhanced item (Built-in pipeline), drawn from the aura mask HeldRimBaker.BakeAura bakes:
// R = closeness to the art (1 at its edge), G = position along the item (0..1), A = band; _Shine.w sets how far out the rim reaches. Alpha-blended, not additive: an additive tint vanishes on light ground
// (orange added to snow is still white), a coloured line reads on snow and on dark ground alike. The renderer colour is the tier colour (rgb)
// and the rim strength (a). _Shine: x = pulse centre, y = pulse half width, z = steady strength (the pulse adds up to 1 on top and whitens).
Shader "ZenoniaProto/SpriteRim"
{
    Properties
    {
        [PerRendererData] _MainTex ("Rim Mask", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Shine ("Pulse centre, half width, steady", Vector) = (-2,0.1,1,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _Shine;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 m = tex2D(_MainTex, i.uv);
                half band = saturate(1.0 - abs(m.g - _Shine.x) / _Shine.y);
                band = band * band * (3.0 - 2.0 * band);
                half3 rgb = lerp(i.color.rgb, half3(1, 1, 1), band * 0.7);   // the pulse runs as a hot, near-white spot along the rim
                half edge = saturate((m.r - (1.0 - _Shine.w)) / 0.08) * m.a; // rim width per band: w = share of the aura reach
                half strength = 0.45 + 0.55 * smoothstep(0.15, 0.6, m.g);   // the blade lights up, the grip stays quiet
                return half4(rgb, saturate(i.color.a * edge * strength * (_Shine.z + band)));
            }
            ENDCG
        }
    }
}
