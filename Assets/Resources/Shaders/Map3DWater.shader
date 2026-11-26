// Stream / pond surface of the 3D map (Map3DWater; ChatGPT spec T14 + round 1 P0-2): transparent sheet a little below the walkable level over the
// sunken bed. Depth is read PER PIXEL from the terrain height texture (_Height: metres on the 0.5 m vertex grid, _HRect / _HSize map a world
// position to it), so the shore is smooth instead of following the mesh triangles. Shallow -> deep colour, two ripple layers scrolling with the flow
// (vertex colour rg), a broken foam band 0.08-0.16 m wide at the shore (alpha 0.3), the sheet fading into the bank over ~0.35 m, stepped glints. Fog.
Shader "ZenoniaProto/Map3DWater"
{
    Properties
    {
        _Noise ("Noise", 2D) = "gray" {}
        _Height ("Height", 2D) = "black" {}
        _HRect ("Height rect", Vector) = (0,0,2,0)
        _HSize ("Height size", Vector) = (0.01,0.01,0,0)
        _Level ("Water level m", Float) = -0.12
        _Shallow ("Shallow", Color) = (0.42,0.76,0.78,0.50)
        _Deep ("Deep", Color) = (0.15,0.45,0.64,0.90)
        _Foam ("Ripple", Color) = (0.88,0.97,0.98,1)
        _DepthRange ("Depth for full deep colour (m)", Float) = 0.45
        _Waves ("Lake waves 0..1", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent-50" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Map3DCommon.cginc"
            sampler2D _Noise, _Height;
            float4 _HRect, _HSize;
            float _Level;
            fixed4 _Shallow, _Deep, _Foam;
            float _Calm, _DepthRange, _Waves;
            // Lake waves (Willowbrook Village, user 25/11 "nước sâu hơn, sóng đánh nhẹ nhàng, hồ nhìn 3D hơn"): height of three slow crossing swells
            // (metres in, metres out) -> normal for shading, a sun glint and a sky fresnel; world: x east, y north, z down (up = -z).
            float SwellH(float2 m, float t)
            {
                return 0.030 * sin(dot(m, float2(0.83, 0.55)) * 1.9 + t * 0.9)
                     + 0.022 * sin(dot(m, float2(-0.45, 0.89)) * 2.7 + t * 1.15)
                     + 0.012 * sin(dot(m, float2(0.97, -0.24)) * 4.6 + t * 1.6);
            }
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float3 wp : TEXCOORD0; float4 col : TEXCOORD1; UNITY_FOG_COORDS(2) };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.col = v.color;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 tile = float2(i.wp.x / 16.0, (_HRect.w - i.wp.y) / 16.0);           // tile coords, y down
                float2 huv = ((tile - _HRect.xy) * _HRect.z + 0.5) * _HSize.xy;
                float ground = tex2Dlod(_Height, float4(huv, 0, 0)).r;
                float dm = _Level - ground;                                                 // metres of water
                clip(dm);
                float depth = saturate(dm / _DepthRange);
                float2 flow = (i.col.rg - 0.5) * 2.0;
                float t = _Time.y;
                float2 w = i.wp.xy;
                float n1 = tex2D(_Noise, w / 70.0 + flow * t * 0.012 * 16.0 / 70.0 * 4.0).g;
                float n2 = tex2D(_Noise, w / 37.0 - flow.yx * t * 0.018 * 16.0 / 37.0 * 4.0 + 0.37).r;
                float n3 = tex2D(_Noise, w / 22.0 + t * 0.01).b;
                float rip = smoothstep(0.60 + 0.05 * _Calm, 0.68 + 0.05 * _Calm, n1 * 0.6 + n2 * 0.4);
                float glint = smoothstep(0.79 + 0.04 * _Calm, 0.82 + 0.04 * _Calm, n2 * 0.5 + n1 * 0.5);
                fixed4 c = lerp(_Shallow, _Deep, smoothstep(0.05, 0.9, depth));
                c.rgb *= _M3DSun.rgb * 0.95;
                float lap = 0.0;
                if (_Waves > 0.0)
                {
                    // RO-style deep water (user 25/11, Ragnarok screenshot "cho nước sâu như này"): saturated blue, nearly opaque, slow mottled
                    // navy / turquoise patches like a deep bed under moving light; a lighter cyan rim only in the shallow band at the bank
                    float2 m = w / 16.0;
                    float mot = tex2D(_Noise, m / 6.0 + t * float2(0.007, 0.004)).g * 0.65 + tex2D(_Noise, m / 2.8 - t * float2(0.004, 0.008) + 0.3).r * 0.35; // round 4: bigger, softer patches
                    float3 lake = lerp(float3(0.06, 0.22, 0.42), float3(0.12, 0.42, 0.64), smoothstep(0.26, 0.58, mot));
                    lake = lerp(lake, float3(0.24, 0.66, 0.80), smoothstep(0.58, 0.82, mot) * 0.85);
                    lake *= lerp(1.0, 0.78, smoothstep(0.5, 1.0, depth));                   // the middle darker still
                    lake = lerp(lake, float3(0.36, 0.74, 0.80), (1.0 - smoothstep(0.04, 0.45, dm)) * 0.65);
                    c.rgb = lerp(c.rgb, lake * (0.55 + 0.5 * _M3DSun.rgb), _Waves);
                    c.a = lerp(c.a, lerp(0.82, 0.97, smoothstep(0.1, 0.6, depth)), _Waves);
                    const float e = 0.08;

                    float dx = (SwellH(m + float2(e, 0), t) - SwellH(m - float2(e, 0), t)) / (2.0 * e);
                    float dy = (SwellH(m + float2(0, e), t) - SwellH(m - float2(0, e), t)) / (2.0 * e);
                    float2 nb = float2(tex2D(_Noise, m / 3.1 + t * 0.03).r, tex2D(_Noise, m / 2.3 - t * 0.025 + 0.5).g) - 0.5; // fine breakup
                    float3 n = normalize(float3(-(dx + nb.x * 0.35), -(dy + nb.y * 0.35), -1.0));
                    float3 L = normalize(float3(-0.55, 0.6, -1.0));                          // sun from the north-west, high
                    float3 V = normalize(_WorldSpaceCameraPos - i.wp);
                    float relief = dot(n, L) - dot(float3(0, 0, -1), L);                    // swell faces toward / away from the sun
                    c.rgb *= 1.0 + relief * 1.5 * _Waves;
                    float fres = pow(1.0 - saturate(dot(n, V)), 4.0);
                    c.rgb = lerp(c.rgb, float3(0.80, 0.92, 1.0), fres * 0.12 * _Waves);  // sky on the far slopes of the swell
                    float spec = pow(saturate(dot(n, normalize(L + V))), 400.0);                 // round 2: 160 x 1.3 washed big white blotches over the lake
                    c.rgb += float3(1.0, 0.96, 0.85) * spec * 0.55 * _Waves;              // sun glint streaks
                    c.a = saturate(c.a + spec * 0.2 * _Waves);
                    // gentle waves lapping the shore: thin foam crests walking in to the bank every ~3 s, fading out over the last 0.3 m of depth
                    float crest = frac(dm * 4.0 + t * 0.33 + n3 * 0.35);
                    lap = smoothstep(0.82, 0.97, crest) * (1.0 - smoothstep(0.04, 0.32, dm)) * smoothstep(0.25, 0.55, n3 + n2 * 0.5) * _Waves;
                }
                c.rgb = lerp(c.rgb, _Foam.rgb, (rip * 0.30 + glint * 0.55) * (1.0 - 0.22 * _Calm) * (1.0 - 0.6 * _Waves)); // the lake has its own glint
                float foamBand = 1.0 - smoothstep(0.03, 0.12 + n3 * 0.05, dm);             // 0.08-0.16 m at the shore, broken by noise
                float foam = foamBand * smoothstep(0.35, 0.6, n3 + n1 * 0.4);
                c.rgb = lerp(c.rgb, _Foam.rgb, foam * (0.85 - 0.45 * _Calm));
                c.rgb = lerp(c.rgb, _Foam.rgb, lap * 0.7);
                c.a = saturate(c.a + rip * 0.15 + glint * 0.3 + foam * 0.3 + lap * 0.35) * smoothstep(0.0, 0.05, dm);
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
}
