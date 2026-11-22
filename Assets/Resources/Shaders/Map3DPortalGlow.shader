// Light parts of the 3D warp gate (Map3DPortals / Map3DPortalMesh, 22/11/2026). One premultiplied transparent shader, three shapes by _Mode:
//  0 vortex: disc in the stone ring, uv -1..1; two layers of the shared noise in swirled polar coordinates turn and drift inward, a bright eye in the
//    middle, a glowing rim just inside the stones, soft edge;
//  1 column: open cylinder (uv.x round, uv.y 0..1 up); soft bands scroll up, faded at the top and towards the silhouette (N.V), so it reads as a
//    volume of light from every camera angle;
//  2 pool: ground disc, uv -1..1; soft glow + a slowly turning circle of runes between two rings.
// Output = body colour x alpha (painted saturated blue, reads on pale roads) + HDR emission (only the eye / runes pass the BloomLite threshold).
// _Breath (per renderer, MaterialPropertyBlock) swings 0.8..1 slowly (MapPortalExits.Breath): nothing blinks.
Shader "ZenoniaProto/Map3DPortalGlow"
{
    Properties
    {
        _Noise ("Noise (r 4, g 9, b 17 cells)", 2D) = "gray" {}
        _RuneTex ("Runes (alpha)", 2D) = "black" {}
        _BodyColor ("Body (rgb, a = max alpha)", Color) = (0.08,0.4,0.95,0.6)
        _EmissionColor ("Emission (HDR)", Color) = (0.1,0.48,1.25,1)
        _Mode ("0 vortex, 1 column, 2 pool", Float) = 0
        _Breath ("Breath", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off ZWrite Off Lighting Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _Noise, _RuneTex;
            float4 _BodyColor, _EmissionColor;
            float _Mode, _Breath;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 n : TEXCOORD1; float3 v : TEXCOORD2; };

            v2f vert(appdata i)
            {
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1.0));
                o.uv = i.uv;
                o.n = UnityObjectToWorldNormal(i.normal);
                o.v = _WorldSpaceCameraPos - wp;
                return o;
            }

            static const float TAU = 6.2831853;

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;
                float alpha = 0.0, glow = 0.0, light = 0.0;
                if (_Mode < 0.5)
                {
                    float r = length(i.uv);
                    float ang = atan2(i.uv.y, i.uv.x) / TAU;
                    float s = ang + r * 0.85 - t * 0.16;                       // swirl: angle bends with the radius and turns
                    float n1 = tex2D(_Noise, float2(s * 3.0, r * 1.1 - t * 0.32)).g;
                    float n2 = tex2D(_Noise, float2(s * 5.0 + 0.37, r * 2.2 - t * 0.55)).b;
                    float arms = smoothstep(0.32, 0.86, n1 * 0.62 + n2 * 0.5);
                    float eye = exp(-r * r * 9.0);
                    float rim = smoothstep(0.74, 0.93, r) * smoothstep(1.0, 0.93, r);
                    float edge = smoothstep(1.0, 0.88, r);
                    alpha = edge * (0.62 + 0.3 * arms);
                    light = arms * 0.55 + rim * 0.35;                          // lighter body where the arms pass
                    glow = edge * (arms * 0.5 + eye * 1.25 + rim * 0.7);
                }
                else if (_Mode < 1.5)
                {
                    float n1 = tex2D(_Noise, float2(i.uv.x * 3.0, i.uv.y * 1.1 - t * 0.22)).g;
                    float n2 = tex2D(_Noise, float2(i.uv.x * 5.0 + 0.5, i.uv.y * 2.4 - t * 0.42)).r;
                    float bands = n1 * 0.6 + n2 * 0.4;
                    float facing = pow(saturate(abs(dot(normalize(i.n), normalize(i.v)))), 1.6);
                    float fade = smoothstep(0.0, 0.07, i.uv.y) * pow(saturate(1.0 - i.uv.y), 1.7);
                    alpha = fade * facing * (0.35 + bands * 0.55);
                    light = bands * 0.4;
                    glow = fade * facing * (0.22 + bands * 0.6);
                }
                else
                {
                    float r = length(i.uv);
                    float ang = atan2(i.uv.y, i.uv.x) / TAU;
                    float soft = pow(saturate(1.0 - r), 2.0);
                    float band = saturate((r - 0.6) / 0.17);                   // the rune ring between r 0.6 and 0.77
                    float inBand = step(0.6, r) * step(r, 0.77);
                    float rune = tex2D(_RuneTex, float2(frac(ang * 2.0 + t * 0.012) , band)).a * inBand; // 16 glyphs round the circle
                    float lines = smoothstep(0.014, 0.0, abs(r - 0.585)) + smoothstep(0.014, 0.0, abs(r - 0.79)) * 0.8;
                    float marks = (rune + lines) * smoothstep(1.0, 0.9, r);
                    alpha = soft * 0.55 + marks * 0.55;
                    light = marks * 0.5;
                    glow = soft * 0.35 + marks * 0.75;
                }
                alpha = saturate(alpha) * _BodyColor.a * lerp(0.85, 1.0, _Breath);
                float3 body = lerp(_BodyColor.rgb, 1.0.xxx, saturate(light) * 0.45);
                float3 c = body * alpha + _EmissionColor.rgb * glow * _Breath;
                return fixed4(c, alpha);
            }
            ENDCG
        }
    }
}
