// Ground shadow composite (FakeSun, GroundShadowRenderer): one quad over the map, drawn just above the ground painting and flat decor and below
// every y-sorted unit, ring and telegraph. Reads the max-opacity mask by world position and multiplies the ground toward the map's shadow colour
// (ground x lerp(1, tint, a), a capped): a cool tint that darkens grass and dirt alike, never black, never stacked. Plain alpha blending of the
// mid-grey tint barely darkened bright grass (first in-game look, 21/11).
// RO3 2.5D maps (globals from View25DLighting, all zero elsewhere so 2D maps are unchanged): sun flecks under canopies (_Dapple25D: holes in the
// shadow that drift slowly, a touch of warm light inside them) and shadows thinning with the distance fog (_Fog25D), so far shadows sit in the haze.
Shader "Hidden/ZenoniaProto/GroundShadowComposite"
{
    Properties { }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off
        Blend DstColor Zero

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _GroundShadowMask;
            float4 _GroundShadowRect;   // xy = world min of the mask, zw = 1 / world size
            float4 _GroundShadowTint;   // rgb = shadow colour, a = alpha cap
            float4 _Dapple25D;          // x = strength (0 = off), y = 1 / spot size (world units), z = drift speed, w = warm boost inside a fleck
            float4 _Fog25D;             // x = fog start distance, y = 1 / (end - start), z = max fog, w = desaturation
            float4 _Sun25D;             // rgb = sun tint of lit things
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = (w.xy - _GroundShadowRect.xy) * _GroundShadowRect.zw;
                o.world = w;
                return o;
            }
            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash21(i), hash21(i + float2(1, 0)), f.x), lerp(hash21(i + float2(0, 1)), hash21(i + float2(1, 1)), f.x), f.y);
            }
            half4 frag(v2f i) : SV_Target
            {
                float inside = step(0.0, i.uv.x) * step(i.uv.x, 1.0) * step(0.0, i.uv.y) * step(i.uv.y, 1.0);
                float a = min(tex2D(_GroundShadowMask, i.uv).r, _GroundShadowTint.a) * inside;
                float3 boost = float3(1, 1, 1);
                if (_Dapple25D.x > 0.0)
                {
                    // sun flecks: two drifting noise layers, kept only where the canopy shadow is deep (not under a lone hero blob edge)
                    float t = _Time.y * _Dapple25D.z;
                    float2 p = i.world.xy * _Dapple25D.y;
                    float n = vnoise(p + float2(t, t * 0.6)) * 0.6 + vnoise(p * 2.3 - float2(t * 0.7, -t)) * 0.4;
                    float fleck = smoothstep(0.66, 0.76, n) * saturate((a - 0.12) / 0.10) * _Dapple25D.x;
                    a *= 1.0 - fleck * 0.85;
                    boost = lerp(float3(1, 1, 1), _Sun25D.rgb * (1.0 + _Dapple25D.w), fleck);
                }
                if (_Fog25D.z > 0.0) a *= 1.0 - saturate((distance(i.world, _WorldSpaceCameraPos) - _Fog25D.x) * _Fog25D.y) * _Fog25D.z;
                return half4(lerp(float3(1, 1, 1), _GroundShadowTint.rgb, a) * boost, 1); // multiply: ground x (1 - a + a * tint)
            }
            ENDCG
        }
    }
}
