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
            float _Calm;
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
                float depth = saturate(dm / 0.45);
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
                c.rgb = lerp(c.rgb, _Foam.rgb, (rip * 0.30 + glint * 0.55) * (1.0 - 0.22 * _Calm));
                float foamBand = 1.0 - smoothstep(0.03, 0.12 + n3 * 0.05, dm);             // 0.08-0.16 m at the shore, broken by noise
                float foam = foamBand * smoothstep(0.35, 0.6, n3 + n1 * 0.4);
                c.rgb = lerp(c.rgb, _Foam.rgb, foam * (0.85 - 0.45 * _Calm));
                c.a = saturate(c.a + rip * 0.15 + glint * 0.3 + foam * 0.3) * smoothstep(0.0, 0.05, dm);
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
}
