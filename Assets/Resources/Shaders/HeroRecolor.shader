// Layered hero recolour (Built-in pipeline, sprite compatible: vertex colour, premultiplied alpha blend, SpriteRenderer + MaterialPropertyBlock).
// The art is painted in neutral greys where the player's colour goes (Tools/build-hero-recolor-mask.py turns the magenta outfit into grey and
// writes _MaskTex: R = hair, G = outfit, same layout as the sheet). In masked pixels the grey lightness L picks a tone on a ramp built from the
// chosen colour: outline-dark -> shadow -> the colour itself at the #999999 base -> a light highlight, so the painted shading stays and dark
// outline pixels stay dark. Unmasked pixels (skin, brown outline, leather, cream shirt) are untouched.
// _HairAll = 1: the whole sprite is a hair layer, every near-grey pixel counts as hair (coloured accents such as goggles keep their colour); no mask needed.
// The same ramp is in Tools/hero_recolor_core.py (ramp()) for the offline previews: change both together.
Shader "ZenoniaProto/HeroRecolor"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _MaskTex ("Mask (R hair, G outfit)", 2D) = "black" {}
        [PerRendererData] _HairColor ("Hair Colour", Color) = (0.6,0.6,0.6,1)
        [PerRendererData] _OutfitColor ("Outfit Colour", Color) = (0.6,0.6,0.6,1)
        [PerRendererData] _HairAll ("Whole sprite is hair", Float) = 0
        _Color ("Tint", Color) = (1,1,1,1)
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
            sampler2D _MaskTex;
            fixed4 _Color, _HairColor, _OutfitColor;
            float _HairAll;

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

            // Ramp knots on the grey lightness (gamma space): #242424 outline, #555555 shadow, #999999 base, #E0E0E0 highlight.
            float3 Ramp(float l, float3 c)
            {
                float3 k0 = c * 0.12;
                float3 k1 = c * 0.24;
                float3 k2 = lerp(c * 0.62, c * c * 0.9, 0.3);
                float3 k4 = lerp(c, float3(1, 1, 1), 0.38);
                float3 k5 = lerp(c, float3(1, 1, 1), 0.65);
                if (l < 0.14) return lerp(k0, k1, l / 0.14);
                if (l < 0.33) return lerp(k1, k2, (l - 0.14) / 0.19);
                if (l < 0.60) return lerp(k2, c, (l - 0.33) / 0.27);
                if (l < 0.88) return lerp(c, k4, (l - 0.60) / 0.28);
                return lerp(k4, k5, saturate((l - 0.88) / 0.12));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 src = tex2D(_MainTex, i.uv);
                float3 rgb = src.rgb;
                float3 hairC = _HairColor.rgb, outC = _OutfitColor.rgb;
            #if !defined(UNITY_COLORSPACE_GAMMA)
                rgb = LinearToGammaSpace(rgb); hairC = LinearToGammaSpace(hairC); outC = LinearToGammaSpace(outC);
            #endif
                fixed4 m = tex2D(_MaskTex, i.uv);
                float chroma = max(rgb.r, max(rgb.g, rgb.b)) - min(rgb.r, min(rgb.g, rgb.b));
                float hairW = _HairAll > 0.5 ? saturate(1 - (chroma - 0.08) / 0.06) : m.r;
                float outW = _HairAll > 0.5 ? 0 : m.g;
                float l = dot(rgb, float3(0.299, 0.587, 0.114));
                rgb = lerp(rgb, Ramp(l, outC), outW);
                rgb = lerp(rgb, Ramp(l, hairC), hairW);
            #if !defined(UNITY_COLORSPACE_GAMMA)
                rgb = GammaToLinearSpace(rgb);
            #endif
                fixed4 c = fixed4(rgb, src.a) * i.color;
                c.rgb *= c.a; // premultiplied alpha (matches Blend One OneMinusSrcAlpha)
                return c;
            }
            ENDCG
        }
    }
}
