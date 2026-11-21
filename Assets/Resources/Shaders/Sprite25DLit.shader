// Lit sprite of a RO3 2.5D map (View25DLighting; ChatGPT RO3 comparison sections 3, 6, 7, 10). One shader, three shared materials so props still batch:
//   _Mode 0 ground   : ground chunks and flat decor: warm sun tint + distance fog.
//   _Mode 1 prop     : standing rocks, houses, logs: + sun-side shading (lit upper left +6 %, shade lower right toward the cool crown-shade colour)
//                      and a soft dark band at the base (fake AO: the prop sits on the ground).
//   _Mode 2 tree     : + crown shadow on the trunk (lower middle darker) and wind sway (upper part only, phase by world position, outer leaves more).
// Props use the whole texture (MapScenery: FullRect sprite, pivot bottom centre), so uv is the position inside the prop. Globals (View25DLighting):
// _Sun25D (sun tint), _CrownShade25D (rgb shade side, a = strength), _Fog25D (start, 1/(end-start), max, desaturation), _Fog25DColor, _Wind25D (amplitude, speed).
// Premultiplied alpha like Sprites/Default; the vertex colour (SpriteRenderer.color, the tree fade) multiplies as usual.
Shader "ZenoniaProto/Sprite25DLit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Mode ("0 ground, 1 prop, 2 tree", Float) = 0
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
            float _Mode;
            float4 _Sun25D, _CrownShade25D, _Fog25D, _Fog25DColor, _Wind25D;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float fog : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                float4 w = mul(unity_ObjectToWorld, v.vertex); // batched sprites are already in world space (identity matrix)
                if (_Mode > 1.5 && _Wind25D.x > 0.0)
                {
                    float bend = smoothstep(0.35, 1.0, v.uv.y);
                    float phase = w.x * 0.045 + w.y * 0.031;
                    float sway = sin(_Time.y * _Wind25D.y + phase) * 0.7 + sin(_Time.y * _Wind25D.y * 2.3 + phase * 1.7) * 0.3;
                    float outer = 0.6 + abs(v.uv.x - 0.5) * 0.8; // outer leaves move more than the crown's core
                    w.x += sway * _Wind25D.x * bend * outer;
                }
                o.pos = mul(UNITY_MATRIX_VP, w);
                o.uv = v.uv;
                o.color = v.color * _Color;
                float d = distance(w.xyz, _WorldSpaceCameraPos);
                o.fog = saturate((d - _Fog25D.x) * _Fog25D.y) * _Fog25D.z;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                if (_Mode > 0.5)
                {
                    // sun from the upper left: lit side up to +6 %, shade side down to the crown-shade colour (strength in _CrownShade25D.a)
                    float lit = saturate(0.55 - (i.uv.x - 0.5) * 0.9 + (i.uv.y - 0.55) * 0.5);
                    float3 shade = lerp(float3(1, 1, 1), _CrownShade25D.rgb * 1.25, _CrownShade25D.a);
                    c.rgb *= lerp(shade, float3(1.06, 1.06, 1.04), lit);
                    // fake AO at the base (bottom ~12 % of the prop): sits on the ground
                    c.rgb *= 1.0 - 0.16 * (1.0 - smoothstep(0.0, 0.12, i.uv.y));
                    if (_Mode > 1.5)
                    {
                        // crown shadow on the trunk: lower middle of a tree, under the crown
                        float trunk = (1.0 - smoothstep(0.08, 0.2, abs(i.uv.x - 0.5))) * smoothstep(0.05, 0.15, i.uv.y) * (1.0 - smoothstep(0.3, 0.45, i.uv.y));
                        c.rgb *= 1.0 - 0.14 * trunk;
                    }
                }
                c.rgb *= _Sun25D.rgb;
                float g = dot(c.rgb, float3(0.299, 0.587, 0.114));
                c.rgb = lerp(c.rgb, float3(g, g, g), i.fog * _Fog25D.w);
                c.rgb = lerp(c.rgb, _Fog25DColor.rgb, i.fog);
                c.rgb *= c.a; // premultiplied alpha
                return c;
            }
            ENDCG
        }
    }
}
