// +15 energy aura of an enhanced item (Built-in pipeline): animated tongues of energy licking outward from the silhouette and
// flowing towards the tip, from the mask HeldRimBaker.BakeAura bakes: R = closeness to the art (1 at its edge), G = position along the item,
// A = band. Premultiplied blend: the coloured body shows on snow, the hot near-white core adds light on dark ground.
// Renderer colour = energy colour (rgb) and strength (a). _Aura (per renderer): x = flow speed, y = tongues along the item, z = tongues across,
// w = reach (share of the baked band, 1 = the full +15 aura).
Shader "ZenoniaProto/SpriteAura"
{
    Properties
    {
        [PerRendererData] _MainTex ("Aura Mask", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Aura ("Flow speed, along, across, reach", Vector) = (1.8, 16, 9, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _Aura;

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

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 m = tex2D(_MainTex, i.uv);
                m.r = saturate((m.r - (1.0 - _Aura.w)) / max(_Aura.w, 0.01)); // per-band reach: w = share of the baked 22 px band
                float t = _Time.y * _Aura.x;
                float2 p = float2(m.g * _Aura.y - t, (i.uv.x + i.uv.y) * _Aura.z + m.r * 1.5);
                float n = vnoise(p) * 0.6 + vnoise(p * 2.13 + float2(7.3, 1.7) - float2(t * 0.6, 0)) * 0.4;
                n = smoothstep(0.25, 0.8, n);                                  // contrast: separate tongues with gaps between them
                float flame = saturate((m.r * 1.7 - (1.0 - n) * 1.25) * 2.2);  // tongues reach further where the noise is high; soft edges, not a solid cut-out
                flame *= smoothstep(0.08, 0.35, m.g) * m.a;                    // quiet at the grip
                flame *= 0.35 + 0.65 * m.r;                                    // thins out towards the tips: see-through, not a solid mass
                half3 col = lerp(i.color.rgb, half3(1, 1, 1), smoothstep(0.55, 1.0, m.r) * 0.5); // lighter right at the blade, colour outward
                half a = flame * i.color.a;
                return half4(col * a, a);
            }
            ENDCG
        }
    }
}
