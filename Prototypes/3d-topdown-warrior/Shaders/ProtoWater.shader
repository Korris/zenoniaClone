// Glade stream (ChatGPT v16): #7FAEAA, alpha 0.72, two soft ripple layers drifting at 0.012 / 0.018 m/s with ~0.1 opacity highlights,
// and a thin pale foam line (0.05-0.1 m) along both banks. UV x = distance across the strip (0 = one bank, 1 = the other), world XZ drives the ripples.
Shader "Proto/Water"
{
    Properties { _Color ("Water", Color) = (0.5, 0.68, 0.67, 0.72) _Ripple ("Ripple opacity", Range(0,0.3)) = 0.1 _Foam ("Foam width (0-1 of strip)", Range(0,0.2)) = 0.05 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color; half _Ripple, _Foam;
            struct v2f { float4 pos : SV_POSITION; float2 across : TEXCOORD0; float3 wpos : TEXCOORD1; };
            v2f vert(appdata_full v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.across = v.texcoord.xy; o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 w = i.wpos.xz; float t = _Time.y;
                float r1 = sin(w.x * 5.1 + w.y * 3.3 + t * 0.012 * 6.283 * 20.0);
                float r2 = sin(w.x * -3.7 + w.y * 6.2 + t * 0.018 * 6.283 * 20.0);
                float ripple = saturate((r1 * r2) * 0.5 + 0.5); ripple = pow(ripple, 6.0);
                fixed4 c = _Color; c.rgb += ripple * _Ripple * 2.0;
                float bank = min(i.across.x, 1.0 - i.across.x);
                float foam = 1.0 - smoothstep(0.0, _Foam, bank);
                c.rgb = lerp(c.rgb, fixed3(0.92, 0.96, 0.93), foam * 0.6); c.a = max(c.a, foam * 0.8);
                return c;
            }
            ENDCG
        }
    }
}
