// Sword-swing ribbon. Mesh UV: x = 0 (tail) .. 1 (head) along the arc, y = 0 (outer blade edge) .. 1 (inner edge).
// _Head (0..1) is how far the swing has progressed, _Fade (0..1) the overall fade-out. Colour: white core -> orange -> red edge.
Shader "ZenoniaProto/TrailRibbon"
{
    Properties
    {
        _Head ("Head", Range(0,1)) = 1
        _Fade ("Fade", Range(0,1)) = 1
        _Intensity ("Intensity", Float) = 1.8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Head;
            float _Fade;
            float _Intensity;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float u = i.uv.x;
                float p = i.uv.y;
                float behind = _Head - u;                                  // >= 0 for the part already swept
                float reveal = saturate(behind * 40.0 + 1.0);              // hard-ish leading edge at the head
                float tailFade = saturate(1.0 - behind * 1.15);            // older parts fade
                float across = 1.0 - smoothstep(0.35, 1.0, p);             // soft inner edge
                float a = reveal * tailFade * across * _Fade;
                float3 orange = float3(1.0, 0.55, 0.15);
                float3 red = float3(0.9, 0.12, 0.1);
                float3 col = lerp(float3(1.0, 1.0, 1.0), orange, smoothstep(0.0, 0.45, p));
                col = lerp(col, red, smoothstep(0.45, 1.0, p));
                return half4(col * _Intensity, a);
            }
            ENDCG
        }
    }
}
